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
    setScrollLeft: function (element, value) {
        element.scrollLeft = value;
    },

    files: {
        _lastHandle: 0,
        _lastTempBuffer: 0,
        _get: function (handle) {
            const file = this[handle];
            if (!file) {
                throw new Error(`Invalid file handle ${id}`);
            }
            return file;
        },
        _tempBuffers: {},

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
            const blob = this._get(handle).slice(position, position + count);
            return String.fromCharCode.apply(null, new Uint8Array(await blob.arrayBuffer()));
        },
        readIntoTempBuffer: async function (handle, position, count) {
            const blob = this._get(handle).slice(position, position + count);
            const tempBufferId = ++this._lastTempBuffer;
            this._tempBuffers[tempBufferId] = new Uint8Array(await blob.arrayBuffer());
            return tempBufferId;
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

    addDefaultPreventingWheelHandler: function (element) {
        element.addEventListener("wheel",
            e => e.deltaY != 0 ? e.preventDefault() : 0, { passive: false });
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
        scrollListItemIntoView: function (listElement, itemIndex, itemHeight) {
            const viewTop = listElement.scrollTop;
            const viewHeight = listElement.getBoundingClientRect().height;
            const viewBottom = viewTop + viewHeight;
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
};
