// Phonebook site JavaScript
(function () {
    'use strict';

    // Mobile sidebar toggle
    var hamburger = document.getElementById('hamburger');
    var sidebar = document.getElementById('sidebar');
    var backdrop = document.getElementById('sidebarBackdrop');

    if (hamburger && sidebar) {
        hamburger.addEventListener('click', function () {
            sidebar.classList.add('open');
        });
    }

    if (backdrop && sidebar) {
        backdrop.addEventListener('click', function () {
            sidebar.classList.remove('open');
        });
    }

    // Close sidebar on escape key
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && sidebar) {
            sidebar.classList.remove('open');
        }
    });

    // Highlight active nav item
    var currentPath = window.location.pathname.toLowerCase();
    var navItems = document.querySelectorAll('.sidebar-nav .nav-item');
    navItems.forEach(function (item) {
        var href = item.getAttribute('href');
        if (href && currentPath.indexOf(href.toLowerCase()) !== -1) {
            item.classList.add('active');
        }
    });
})();
