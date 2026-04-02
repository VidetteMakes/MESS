window.scrollTo = function(elementId) {
    const el = document.getElementById(elementId);
    if (el) {
        el?.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }
}

window.ScrollToTop = function() {
    window.scrollTo({top: 0, left: 0, behavior: 'smooth'});
}