/*
** NOTE: This file is generated by Gulp and should not be edited directly!
** Any changes made directly to this file will be overwritten next time its asset group is processed by Gulp.
*/

// We add some classes to the body tag to restore the sidebar to the state is was before reload.
// That state was saved to localstorage by userPreferencesPersistor.js
// We need to apply the classes BEFORE the page is rendered. 
// That is why we use a MutationObserver instead of document.Ready().
var observer = new MutationObserver(function (mutations) {
    for (var i = 0; i < mutations.length; i++) {
        for (var j = 0; j < mutations[i].addedNodes.length; j++) {
            if (mutations[i].addedNodes[j].tagName == 'BODY') {

                var body = mutations[i].addedNodes[j];

                var adminPreferences = JSON.parse(localStorage.getItem('adminPreferences'));
                if (adminPreferences != null) {
                    if (adminPreferences.leftSidebarCompact == true) {
                        body.className += ' left-sidebar-compact';
                    }
                }
                // we're done: 
                observer.disconnect();
            };
        }
    }
});

observer.observe(document.documentElement, {
    childList: true,
    subtree: true
});
