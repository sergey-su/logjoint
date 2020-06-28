window.logjoint = {
    getElementWidth: function (e) {
        return e.getBoundingClientRect().width;
    },
    getElementHeight: function (e) {
        return e.getBoundingClientRect().height;
    },
    getElementLeft: function (e) {
        return e.getBoundingClientRect().left;
    }
};
