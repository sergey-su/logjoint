window.logjoint = {
    getResourceUrl: function (resourceName) {
        return (new URL(resourceName, window.location)).href;
    },

    layout: {
        getElementWidth: function (e) {
            return e.getBoundingClientRect().width;
        },
        getElementHeight: function (e) {
            return e.getBoundingClientRect().height;
        },
        getElementScrollerHeight(e) {
            const style = document.defaultView.getComputedStyle(e, '::-webkit-scrollbar');
            return (style && style.height) ? parseInt(style.height, 10) : 0;
        },
        getElementOffsetTop: function (e) {
            return e.offsetTop;
        },
        getElementOffsetLeft: function (e) {
            return e.offsetLeft;
        },
    },

    scroll: {
        scrollIntoView: function (element) {
            element.scrollIntoView({
                behavior: 'smooth',
                block: 'nearest',
                inline: 'nearest',
            });
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
        getScrollTop: function (e) {
            return e.scrollTop;
        },
        getScrollLeft: function (e) {
            return e.scrollLeft;
        },
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
            return new Uint8Array(await blob.arrayBuffer());
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
        openBlobFromDb: async function (key) {
            const blob = await logjoint.db.get('blobs', key);
            if (!blob) {
                throw new Error(`Can not open blob from db by key '${key}'`);
            }
            const handle = ++this._lastHandle;
            this[handle] = blob;
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
            return tempBuffer;
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

            const captureMouse = (e) => {
                resizer.setPointerCapture(e.pointerId);
                e.stopPropagation();
            }

            resizer.addEventListener('mousedown', startResize, false);
            resizer.addEventListener('pointerdown', captureMouse, false);
        },

        initEWResizer: function (resizer, target, inverse, relativeToParent, setter) {
            this._initResizer(resizer, target, (startWidth, startHeight, deltaWidth, deltaHeight) => {
                const newWidthPx = startWidth + (inverse ? -1 : 1) * deltaWidth;
                if (relativeToParent) {
                    const parentWidthPx = parseInt(document.defaultView.getComputedStyle(target.parentElement).width);
                    setter.invokeMethodAsync('Invoke', newWidthPx / parentWidthPx);
                } else {
                    setter.invokeMethodAsync('Invoke', newWidthPx);
                }
            });
        },

        initNSResizer: function (resizer, target, inverse, relativeToParent, setter) {
            this._initResizer(resizer, target, (startWidth, startHeight, deltaWidth, deltaHeight) => {
                const newHeightPx = startHeight + (inverse ? -1 : 1) * deltaHeight;
                if (relativeToParent) {
                    const parentHeightPx = parseInt(document.defaultView.getComputedStyle(target.parentElement).height);
                    setter.invokeMethodAsync('Invoke', newHeightPx / parentHeightPx);
                } else {
                    setter.invokeMethodAsync('Invoke', newHeightPx);
                }
            });
        },
    },

    keyboard: {
        addHandler: function (element, keysStr, handler, preventDefault, stopPropagation) {
            const keys = keysStr.map(keyStr => {
                const split = keyStr.split("+");
                if (split.length < 1) {
                    throw new Error(`Bad key string: '${preventedKeyStr}'`);
                }
                const keySplit = split[split.length - 1].split('/');
                const keyOptions = keySplit[1] || '';
                return {
                    key: keySplit[0],
                    caseInsensitive: keyOptions.includes('i'),
                    modifiers: split.slice(0, split.length - 1),
                };
            });
            element.addEventListener('keydown', e => {
                for (const key of keys) {
                    const norm = k => key.caseInsensitive ? k.toLowerCase() : k;
                    if (norm(key.key) == norm(e.key)) {
                        let allMatch = true;
                        for (const modifier of key.modifiers) {
                            const modifierMatch =
                                (modifier == "Control" || modifier == "Ctrl") ? e.ctrlKey :
                                modifier == "Alt" ? e.altKey :
                                modifier == "Shift" ? e.shiftKey :
                                modifier == "Meta" ? e.metaKey :
                                modifier == "Edit" ? (e.metaKey || e.ctrlKey):
                                true;
                            allMatch = allMatch && modifierMatch;
                        }
                        if (allMatch) {
                            if (preventDefault) {
                                e.preventDefault();
                            }
                            if (stopPropagation) {
                                e.stopPropagation();
                            }
                            if (handler) {
                                handler.invokeMethodAsync('Invoke');
                            }
                            break;
                        }
                    }
                }
            }, false);
        },
    },

    style: {
        adoptStyle: function (cssString) {
            const sheet = new CSSStyleSheet();
            sheet.replaceSync(cssString);
            document.adoptedStyleSheets = [...document.adoptedStyleSheets, sheet];
        },

        getComputedStyle: function (e, prop) {
            return document.defaultView.getComputedStyle(e).getPropertyValue(prop);
        },

        setProperty: function (e, prop, value) {
            e.style.setProperty(prop, value);
        },
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
        focusPrimaryListItem: function (listElement) {
            if (logjoint.focus.isFocusWithin(listElement)) {
                const primary = listElement.querySelector('li.primary');
                if (primary) {
                    primary.focus();
                }
            }
        },
    },

    tree: {
        focusPrimaryTreeNode: function (treeElement, allowFocusStealing) {
            if (allowFocusStealing || logjoint.focus.isFocusWithin(treeElement)) {
                const primary = treeElement.querySelector('.node.primary');
                if (primary) {
                    primary.focus();
                }
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
                const handles = await Promise.all(
                    getFileItems(ev.dataTransfer).map(
                        item => item.getAsFileSystemHandle().then(
                            nativeHandle => logjoint.nativeFiles.add(nativeHandle))));
                await handler.invokeMethodAsync('HandleDrop', handles);
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

    logViewer: {
        initVScroller: function (slider, handler) {
            let startY, startTop, startParentHeight;
    
            const doScroll = (e) => {
                if (startParentHeight != 0) {
                    handler.invokeMethod('OnVScroll', (startTop + (e.clientY - startY)) / startParentHeight);
                }
                e.preventDefault();
            };

            const stopScroll = () => {
                document.documentElement.removeEventListener('mousemove', doScroll, false);
                document.documentElement.removeEventListener('mouseup', stopScroll, false);
            };

            const startScroll = (e) => {
                startY = e.clientY;
                startTop = parseInt(document.defaultView.getComputedStyle(slider).top, 10);
                startParentHeight = parseInt(document.defaultView.getComputedStyle(slider.parentElement).height, 10);
                document.documentElement.addEventListener('mousemove', doScroll, false);
                document.documentElement.addEventListener('mouseup', stopScroll, false);
            };

            slider.addEventListener('mousedown', startScroll, false);
        }
    },

    timelineVisualizer: {
        addDefaultPreventingWheelHandler: function (element) {
            const handler = e => {
                if (e.deltaY != 0 && !e.ctrlKey) {
                    return;
                }
                e.preventDefault();
            };
            element.addEventListener("wheel", handler, false);
        },
        setMouseCapturingHandler: function (mainElement) {
            const handler = evt => {
                for (let e = evt.target; e && e != mainElement; e = e.parentElement) {
                    if (e.className.includes("t")) {
                        return; // triggers are excluded from pointer capturing
                    }
                }
                mainElement.setPointerCapture(evt.pointerId);
            };
            mainElement.addEventListener('pointerdown', handler, false);
        },
    },

    mouse: {
        setMouseCapturingHandler: function (element) {
            const handler = e => element.setPointerCapture(e.pointerId);
            element.addEventListener('pointerdown', handler, false);
        },
    },

    selection: {
        getSelectedTextInElement: function (element) {
            const selection = window.getSelection();
            const isParentedByElement = n =>
                !n ? false :
                n === element ? true :
                isParentedByElement(n.parentElement);
            if (selection && isParentedByElement(selection.anchorNode) && isParentedByElement(selection.focusNode)) {
                return selection.toString();
            }
            return null;
        }
    },

    browser: {
        isMac: function() {
            return navigator.appVersion.indexOf("Mac") > -1;
        },
    },

    focus: {
        isFocusWithin: function (element) {
            return !!element && element.contains(document.activeElement);
        },

        getFocusedElementTag: function() {
            return document.activeElement ? document.activeElement.tagName : "";
        },

        trapFocusInModal: function (modalElement) {
            let lastFocusedModalDescendent;
            let suppressFocusHandling;

            function enumFocusableDescendants(node) {
                const focusableQuery =
                     `a[href]:not([tabindex='-1']),
                      area[href]:not([tabindex='-1']),
                      input:not([disabled]):not([tabindex='-1']),
                      select:not([disabled]):not([tabindex='-1']),
                      textarea:not([disabled]):not([tabindex='-1']),
                      button:not([disabled]):not([tabindex='-1']),
                      iframe:not([tabindex='-1']),
                      [tabindex]:not([tabindex='-1']),
                      [contentEditable=true]:not([tabindex='-1'])`
                return node.querySelectorAll(focusableQuery);
            }

            function handleDocumentFocus(event) {
                if (suppressFocusHandling) {
                    return;
                }
                if (modalElement.contains(event.target)) {
                    lastFocusedModalDescendent = event.target;
                } else {
                    suppressFocusHandling = true;
                    try {
                        const focusable = enumFocusableDescendants(modalElement);
                        if (focusable.length >= 1) {
                            focusable[0].focus();
                            if (document.activeElement === lastFocusedModalDescendent) {
                                focusable[focusable.length - 1].focus();
                            }
                            lastFocusedModalDescendent = document.activeElement;
                        }
                    } finally {
                        suppressFocusHandling = false;
                    }
                }
            }

            function focusInitialDescendant() {
                const focusable = enumFocusableDescendants(modalElement);
                if (focusable.length >= 1) {
                    focusable[0].focus();
                }
            }

            focusInitialDescendant();

            document.addEventListener('focus', handleDocumentFocus, true);

            return {
                dispose: () => document.removeEventListener('focus', handleDocumentFocus, true)
            }
        },
    },

    chrome_extension: {
        _port: undefined,
        init: function(callback) {
            const connectInfo = {
                name: "logjoint.wasm",
            };
            const connect = () => {
                let lastErrror = undefined;
                for (let extId of [
                    "hakgmeclhiipohohmoghhmbjlicdnbbb" // dev extension id
                ]) {
                    try {
                        this._port = chrome.runtime.connect(extId, connectInfo);
                    } catch (e) {
                        lastErrror = e;
                        continue;
                    }
                }
                if (this._port) {
                    console.log('Connected to chrome extension');
                    this._port.onMessage.addListener(async function(msg) {
                        if (msg.type === "open_log") {
                            const db = window.logjoint.db;
                            const id = encodeURIComponent(msg.id);
                            console.log("Got open log request from chrome extension. Log len=", msg.text.length, " id=", id);
                            const blob = new Blob([msg.text]);
                            await db.set("blobs", blob, id);
                            callback.invokeMethodAsync('Open', id, msg.displayName || msg.id);
                        }
                    });
                    this._port.onDisconnect.addListener((p) => {
                        console.log('Got disconnected from chrome extension',
                            p.error ? ` with error ${p.error.message}` : '', '. Reconnecting soon.');
                        this._port = undefined;
                        setTimeout(connect, 1000);
                    });
                } else {
                    console.log('Failed to connect to chrome extension', lastErrror);
                }
            }
            connect();
            return !!this._port;
        }
    }
};
