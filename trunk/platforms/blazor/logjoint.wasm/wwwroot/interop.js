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
        _get: function (handle) {
            const file = this[handle];
            if (!file) {
                throw new Error(`Invalid file handle ${id}`);
            }
            return file;
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
            const blob = this._get(handle).slice(position, position + count);
            return String.fromCharCode.apply(null, new Uint8Array(await blob.arrayBuffer()));
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

        initNWResizer: function (resizer, target, inverse) {
            let startX, startWidth;

            const doResize = (e) => {
                target.style.width = `${startWidth + (inverse ? -1 : 1) * (e.clientX - startX)}px`;
                e.preventDefault();
            };

            const stopResize = () => {
                document.documentElement.removeEventListener('mousemove', doResize, false);
                document.documentElement.removeEventListener('mouseup', stopResize, false);
            };

            const startResize = (e) => {
                startX = e.clientX;
                startWidth = parseInt(document.defaultView.getComputedStyle(target).width, 10);
                document.documentElement.addEventListener('mousemove', doResize, false);
                document.documentElement.addEventListener('mouseup', stopResize, false);
            };

            resizer.addEventListener('mousedown', startResize, false);
        },
    },

    addDefaultPreventingWheelHandler: function (element) {
        element.addEventListener("wheel",
            e => e.deltaY != 0 ? e.preventDefault() : 0, { passive: false });
    }
};
