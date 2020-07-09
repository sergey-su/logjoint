window.logjoint = {
    getElementWidth: function (e) {
        return e.getBoundingClientRect().width;
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
};
