window.SortableListInit = function init(elOrRef, group, pull, put, sort, handle, filter, component, forceFallback) {
    // Helper to get DOM element (either ElementReference marshalled by Blazor or an id string)
    function getElement(candidate) {
        if (!candidate) return null;
        // If Blazor passed an ElementReference, it will be the actual element here
        if (candidate instanceof HTMLElement) return candidate;
        // otherwise if it's a string id
        if (typeof candidate === 'string') return document.getElementById(candidate);
        // could be a JS object wrapper from Blazor; try to read .then? but usually instance check is enough
        return null;
    }

    const MAX_TRIES = 10;
    const TRY_DELAY_MS = 40;
    let tries = 0;

    function tryInit() {
        const root = getElement(elOrRef);
        if (!root) {
            tries++;
            if (tries <= MAX_TRIES) {
                setTimeout(tryInit, TRY_DELAY_MS);
                return;
            } else {
                console.warn('SortableList.init: element not found after retries', elOrRef);
                return;
            }
        }

        // avoid double-init on same element
        if (root._sortable) {
            // already initialized, nothing more to do
            return;
        }

        const sortable = new Sortable(root, {
            animation: 200,
            group: {
                name: group,
                pull: pull || true,
                put: put
            },
            filter: filter || undefined,
            sort: sort,
            forceFallback: forceFallback,
            handle: handle || undefined,
            onUpdate: (event) => {
                // revert DOM to .NET state (existing behavior)
                event.item.remove();
                event.to.insertBefore(event.item, event.to.childNodes[event.oldIndex]);

                // Notify .NET to update its model and re-render
                component.invokeMethodAsync('OnUpdateJS', event.oldDraggableIndex, event.newDraggableIndex);
            },
            onRemove: (event) => {
                if (event.pullMode === 'clone') {
                    event.clone.remove();
                }

                event.item.remove();
                event.from.insertBefore(event.item, event.from.childNodes[event.oldIndex]);

                component.invokeMethodAsync('OnRemoveJS', event.oldDraggableIndex, event.newDraggableIndex);
            }
        });

        // store a reference for cleanup
        root._sortable = sortable;
    }

    tryInit();
}

// cleanup function to call from .NET
window.SortableListDestroy = function destroy(elOrRef) {
    function getElement(candidate) {
        if (!candidate) return null;
        if (candidate instanceof HTMLElement) return candidate;
        if (typeof candidate === 'string') return document.getElementById(candidate);
        return null;
    }

    const root = getElement(elOrRef);
    if (root && root._sortable) {
        try {
            root._sortable.destroy();
        } catch (e) {
            console.warn('SortableList.destroy: error destroying sortable', e);
        }
        delete root._sortable;
    }
}