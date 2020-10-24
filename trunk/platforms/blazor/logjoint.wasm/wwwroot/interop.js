window.logjoint = {
    getElementWidth: function (e) {
        return e.getBoundingClientRect().width;
    },
    setElementWidth: function (e, value) {
        e.style.width = `${value}px`;
    },
    getElementHeight: function (e) {
        return e.getBoundingClientRect().height;
    },
    getElementLeft: function (e) {
        return e.getBoundingClientRect().left;
    },
    getResourceUrl: function (resourceName) {
        return (new URL(resourceName, window.location)).href;
    },
    scrollLeftIntoView: function (element, targetX) {
        const w = element.getBoundingClientRect().width;
        const x = element.scrollLeft;
        if (targetX < x) {
            element.scrollLeft = targetX;
        } else if (targetX > x + w) {
            element.scrollLeft = targetX - w;
        }
    },

    files: {
        _lastHandle: 0,
        _lastTempBuffer: 0,
        _get: function (handle) {
            const file = this[handle];
            if (!file) {
                throw new Error(`Invalid file handle ${handle}`);
            }
            return file;
        },
        _tempBuffers: {},
        _readFile: async function (file, position, count) {
            const blob = file.slice(position, position + count);
            return String.fromCharCode.apply(null, new Uint8Array(await blob.arrayBuffer()));
        },
        _readIntoTempBuffer: async function (file, position, count) {
            const blob = file.slice(position, position + count);
            const array = new Uint8Array(await blob.arrayBuffer())
            const tempBufferId = ++this._lastTempBuffer;
            this._tempBuffers[tempBufferId] = array;
            return tempBufferId;
        },
        _allocateEmptyTempBuffer: function () {
            const tempBufferId = ++this._lastTempBuffer;
            this._tempBuffers[tempBufferId] = new Uint8Array();
            return tempBufferId;
        },

        open: function (fileInput) {
            const file = fileInput.files[0];
            if (!file) {
                throw new Error(`Can not open file: <input> has no files`);
            }
            const handle = ++this._lastHandle;
            this[handle] = file;
            return handle;
        },
        close: function (handle) {
            this._get(handle);
            delete this[handle];
        },
        getSize: function (handle) {
            return this._get(handle).size;
        },
        getName: function (handle) {
            return this._get(handle).name;
        },
        getLastModified: function (handle) {
            return this._get(handle).lastModified;
        },
        read: async function (handle, position, count) {
            return this._readFile(this._get(handle), position, count);
        },
        readIntoTempBuffer: async function (handle, position, count) {
            return this._readIntoTempBuffer(this._get(handle), position, count);
        },
        readTempBuffer: function (tempBufferId) {
            const tempBuffer = this._tempBuffers[tempBufferId];
            if (!tempBuffer) {
                throw new Error(`Temp buffer ${tempBufferId} does not exist`);
            }
            delete this._tempBuffers[tempBufferId];
            return BINDING.js_typed_array_to_array(tempBuffer);
        },
        getUrl: function (fileInput) {
            const file = fileInput.files[0];
            if (!file) {
                throw new Error(`Can not open file: <input> has no files`);
            }
            return URL.createObjectURL(file);
        },
    },

    // https://web.dev/native-file-system/
    nativeFiles: {
        _lastHandle: 0,
        _get: function (handle) {
            const nativeHandle = this[handle];
            if (!nativeHandle) {
                throw new Error(`Invalid native file handle ${handle}`);
            }
            return nativeHandle;
        },
        _makeDetached: function(entry) {
            entry.file = undefined;
            entry.size = 0;
        },
        _refresh: async function(entry, initialRefresh) {
            try {
                const permissionOptions = {
                    mode: 'read'
                };
                let permitted = await entry.nativeHandle.queryPermission(permissionOptions) === 'granted';
                if (!permitted) {
                    permitted = await entry.nativeHandle.requestPermission(permissionOptions) === 'granted';
                }
                if (permitted) {
                    entry.file = await entry.nativeHandle.getFile();
                    entry.name = entry.file.name;
                    entry.size = entry.file.size;
                    entry.lastModified = entry.file.lastModified;
                } else if (initialRefresh) {
                    throw new Error("Access not permitted by user");
                } else {
                    this._makeDetached(entry);
                }
            } catch (e) {
                if (!initialRefresh && (e.name === "NotReadableError" || e.name === "NotFoundError")) {
                    this._makeDetached(entry);
                } else {
                    throw e;
                }
            }
        },
        _read: async function(handle, readCallback, noFileCallback) {
            const entry = this._get(handle);
            for (let attempt = 0;;++attempt) {
                if (!entry.file) {
                    return noFileCallback();
                } else {
                    try {
                        return await readCallback(entry.file);
                    } catch (e) {
                        if (attempt > 0) {
                            throw e;
                        } else {
                            await this._refresh(entry, /*initialRefresh=*/false);
                        }
                    }
                }
            }
        },
        _add: async function(nativeHandle) {
            const entry = {
                nativeHandle: nativeHandle,
                file: undefined,
                name: undefined,
                size: undefined,
                lastModified: undefined
            };
            await this._refresh(entry, /*initialRefresh=*/true);
            const handle = ++this._lastHandle;
            this[handle] = entry;
            return handle;
        },

        isSupported: function() {
            // todo: check support
            return true;
        },
        choose: async function () {
            const [nativeHandle] = await window.showOpenFilePicker();
            return await this._add(nativeHandle);
        },
        add: async function(nativeHandle) {
            return await this._add(nativeHandle);
        },
        close: function (handle) {
            this._get(handle);
            delete this[handle];
        },
        getSize: function (handle) {
            return this._get(handle).size;
        },
        getName: function (handle) {
            return this._get(handle).name;
        },
        getLastModified: function (handle) {
            return this._get(handle).lastModified;
        },
        read: async function (handle, position, count) {
            return this._read(handle,
                file => logjoint.files._readFile(file, position, count),
                () => ''
            );
        },
        readIntoTempBuffer: async function (handle, position, count) {
            return this._read(handle,
                file => logjoint.files._readIntoTempBuffer(file, position, count),
                logjoint.files._allocateEmptyTempBuffer
            );
        },
        readTempBuffer: function (tempBufferId) {
            return logjoint.files.readTempBuffer(tempBufferId);
        },
        ensureStoredInDatabase: async function(handle) {
            const entry = this._get(handle);
            const db = window.logjoint.db;
            for (const record of await db.queryIndex("file-handles", "name", entry.name)) {
                if (await entry.nativeHandle.isSameEntry(record.fileHandle)) {
                    return record.id;
                }
            }
            return await db.set("file-handles", {
                name: entry.name,
                fileHandle: entry.nativeHandle,
            });
        },
        restoreFromDatabase: async function(dbId) {
            const db = window.logjoint.db;
            const record = await db.get("file-handles", dbId);
            return await this._add(record.fileHandle);
        },
    },

    saveAs: function(content, name) {
        let a = document.createElement('a');
        a.download = name;
        a.rel = 'noopener';
        let blob = new Blob([content], { type: 'text/plain' });
        a.href = URL.createObjectURL(blob);
        setTimeout(function () { URL.revokeObjectURL(a.href); }, 60000);
        setTimeout(function () { a.dispatchEvent(new MouseEvent('click')); }, 0);
    },

    getLocalStorageItem: function(key) {
        return window.localStorage.getItem(key);
    },
    setLocalStorageItem: function(key, value) {
        return window.localStorage.setItem(key, value);
    },

    resize: {
        _lastHandle: 0,
        observe: function (element, handler) {
            const handle = ++this._lastHandle;
            this[handle] = new ResizeObserver(entries => {
                handler.invokeMethod('OnResize');
            });
            this[handle].observe(element);
            return handle;
        },
        unobserve: function (handle) {
            if (this[handle]) {
                this[handle].disconnect();
                delete this[handle];
            }
        },

        _initResizer: function (resizer, target, resize) {
            let startX, startY, startWidth, startHeight;

            const doResize = (e) => {
                resize(startWidth, startHeight, e.clientX - startX, e.clientY - startY);
                e.preventDefault();
            };

            const stopResize = () => {
                document.documentElement.removeEventListener('mousemove', doResize, false);
                document.documentElement.removeEventListener('mouseup', stopResize, false);
            };

            const startResize = (e) => {
                startX = e.clientX;
                startY = e.clientY;
                const style = document.defaultView.getComputedStyle(target);
                startWidth = parseInt(style.width, 10);
                startHeight = parseInt(style.height, 10);
                document.documentElement.addEventListener('mousemove', doResize, false);
                document.documentElement.addEventListener('mouseup', stopResize, false);
            };

            resizer.addEventListener('mousedown', startResize, false);
        },

        initEWResizer: function (resizer, target, inverse, relativeToParent) {
            this._initResizer(resizer, target, (startWidth, startHeight, deltaWidth, deltaHeight) => {
                const newWidthPx = startWidth + (inverse ? -1 : 1) * deltaWidth;
                if (relativeToParent) {
                    const parentWidthPx = parseInt(document.defaultView.getComputedStyle(target.parentElement).width);
                    target.style.width = `${100 * newWidthPx / parentWidthPx}%`;
                } else {
                    target.style.width = `${newWidthPx}px`;
                }
            });
        },

        initNSResizer: function (resizer, target, inverse, relativeToParent) {
            this._initResizer(resizer, target, (startWidth, startHeight, deltaWidth, deltaHeight) => {
                const newHeightPx = startHeight + (inverse ? -1 : 1) * deltaHeight;
                if (relativeToParent) {
                    const parentHeightPx = parseInt(document.defaultView.getComputedStyle(target.parentElement).height);
                    target.style.height = `${100 * newHeightPx / parentHeightPx}%`;
                } else {
                    target.style.height = `${newHeightPx}px`;
                }
            });
        },
    },

    addDefaultPreventingKeyHandler: function (element, preventingKeys) {
        element.addEventListener('keydown', e => {
            if (preventingKeys.indexOf(e.key) >= 0) {
                e.preventDefault();
            }
        }, false);
    },

    adoptStyle: function (cssString) {
        const sheet = new CSSStyleSheet();
        sheet.replaceSync(cssString);
        document.adoptedStyleSheets = [...document.adoptedStyleSheets, sheet];
    },

    list: {
        scrollListItemIntoView: function (listElement, itemIndex) {
            const firstItem = listElement.firstElementChild;
            if (!firstItem) {
                return; // empty list, nothing to scroll
            }
            const viewTop = listElement.scrollTop;
            const viewHeight = listElement.getBoundingClientRect().height;
            const viewBottom = viewTop + viewHeight;
            const itemHeight = firstItem.getBoundingClientRect().height;
            const itemTop = itemIndex * itemHeight;
            const itemBottom = (itemIndex + 1) * itemHeight;
            if (itemBottom > viewBottom || itemTop < viewTop) {
                listElement.scrollTop = (itemTop + itemBottom) / 2 - viewHeight / 2;
            }
        },
        focusSelectedListItem: function (listElement) {
            const selected = listElement.querySelector('li.selected');
            if (selected) {
                selected.focus();
            }
        },
    },

    db: {
        // the singleton promise that resolves when the database is open
        _dbOpen: undefined,
        // resolves when the transation completes
        _transact: function(storeName, mode, transactionBody) {
            if (!this._dbOpen) {
                this._dbOpen = new Promise((resolve, reject) => {
                    const request = window.indexedDB.open('logjoint', 1);
                    request.onsuccess = () => resolve(request.result);
                    request.onerror = () => reject(request.error);
                    request.onupgradeneeded = (event) => {
                        const db = request.result;
                        if (event.oldVersion < 1) {
                            const fileHandlesStore = db.createObjectStore(
                                'file-handles', { keyPath: 'id', autoIncrement: true });
                            fileHandlesStore.createIndex("name", "name");

                            db.createObjectStore('blobs');
                        }
                    };
                });
            }
            return this._dbOpen.then(db => new Promise((resolve, reject) => {
                const transaction = db.transaction(storeName, mode);
                transaction.oncomplete = () => resolve();
                transaction.onabort = transaction.onerror = () => reject(transaction.error);
                transactionBody(transaction.objectStore(storeName));
            }));
        },

        get: function(storeName, key) {
            let getRequest;
            return this._transact(storeName, 'readonly', store => {
                getRequest = store.get(key);
            }).then(() => getRequest.result);
        },
        set: function(storeName, value, key) {
            let putRequest;
            return this._transact(storeName, 'readwrite', store => {
                putRequest = store.put(value, key);
            }).then(() => putRequest.result);
        },
        queryIndex: function(storeName, indexName, value) {
            const result = [];
            return this._transact(storeName, 'readonly', store => {
                const index = store.index(indexName);
                const custorRequest = index.openCursor(value);
                custorRequest.onsuccess = function(event) {
                    const cursor = event.target.result;
                    if (cursor) {
                        result.push(cursor.value);
                        cursor.continue();
                    }
                };
            }).then(() => result);
        },
        keys: function(storeName) {
            const result = [];
            return this._transact(storeName, 'readonly', store => {
                store.openKeyCursor().onsuccess = (event) => {
                    const cursor = event.target.result;
                    if (cursor) {
                        result.push(cursor.key);
                        cursor.continue();
                    }
                };
            }).then(() => result);
        },
    },

    clipboard: {
        setText: function (value) {
            return navigator.clipboard.writeText(value);
        }
    },

    dragDrop: {
        registerHandler: function(element, overlayClass, handler) {
            const overlay = document.getElementsByClassName("drag-drop-overlay")[0];
            if (!overlay) {
                throw new Error(`drag drop overlay ${overlayClass} not found`);
            }
    
            function getFileItems(dataTransfer) {
                return [...(dataTransfer.items || [])].filter(i => i.kind === 'file');
            }
    
            async function dropHandler(ev) {
                ev.preventDefault();
                for (const item of getFileItems(ev.dataTransfer)) {
                    const nativeHandle = await item.getAsFileSystemHandle();
                    const handle = await logjoint.nativeFiles.add(nativeHandle);
                    console.log('item handle', handle);
                }
            }
    
            let dragTimeout = undefined;
            function dragOverHandler(ev) {
                const firstDrag = dragTimeout == undefined;
                if (dragTimeout) {
                    clearTimeout(dragTimeout);
                }
                if (getFileItems(ev.dataTransfer).length > 0) {
                    if (firstDrag) {
                        overlay.style.display = "block";
                    }
                    dragTimeout = setTimeout(() => {
                        dragTimeout = undefined;
                        overlay.style.display = "none";
                    }, 100);
                    ev.dataTransfer.dropEffect = "copy";
                } else {
                    ev.dataTransfer.dropEffect = "none";
                }
                ev.preventDefault();
            }
    
            element.addEventListener('dragover', dragOverHandler);
            element.addEventListener('drop', dropHandler);
        }
    },
};
