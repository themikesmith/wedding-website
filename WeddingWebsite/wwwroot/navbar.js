// Navbar overflow and mode logic (moved from Navbar.razor)
window.weddingNavbar = window.weddingNavbar || {};
(function(exports) {
    if (exports.__initialized) return;
    exports.__initialized = true;

    // All state is now on the exports object
    exports.currentNavbarMode = exports.currentNavbarMode || 'desktop';
    exports.navbarModeUpdateQueued = exports.navbarModeUpdateQueued || false;

    function withTemporaryDesktopMeasurement(desktopNavbar, measureFunc) {
        const computedStyles = window.getComputedStyle(desktopNavbar);
        const isHidden = computedStyles.display === 'none' || desktopNavbar.clientWidth === 0;
        if (!isHidden) {
            return measureFunc();
        }
        const previousInlineStyles = {
            display: desktopNavbar.style.display,
            position: desktopNavbar.style.position,
            visibility: desktopNavbar.style.visibility,
            left: desktopNavbar.style.left,
            top: desktopNavbar.style.top,
            width: desktopNavbar.style.width,
            pointerEvents: desktopNavbar.style.pointerEvents
        };
        desktopNavbar.style.setProperty('display', 'flex', 'important');
        desktopNavbar.style.position = 'fixed';
        desktopNavbar.style.visibility = 'hidden';
        desktopNavbar.style.left = '0';
        desktopNavbar.style.top = '-9999px';
        desktopNavbar.style.width = '100vw';
        desktopNavbar.style.pointerEvents = 'none';
        const result = measureFunc();
        desktopNavbar.style.display = previousInlineStyles.display;
        desktopNavbar.style.position = previousInlineStyles.position;
        desktopNavbar.style.visibility = previousInlineStyles.visibility;
        desktopNavbar.style.left = previousInlineStyles.left;
        desktopNavbar.style.top = previousInlineStyles.top;
        desktopNavbar.style.width = previousInlineStyles.width;
        desktopNavbar.style.pointerEvents = previousInlineStyles.pointerEvents;
        return result;
    }

    function getDesktopWidthMetrics(desktopNavbar) {
        if (!desktopNavbar) {
            return { requiredWidth: 0, availableWidth: 0 };
        }
        return withTemporaryDesktopMeasurement(desktopNavbar, () => {
            const navbarStyles = window.getComputedStyle(desktopNavbar);
            let requiredWidth = 0;
            for (const child of desktopNavbar.children) {
                if (!(child instanceof HTMLElement)) continue;
                const styles = window.getComputedStyle(child);
                if (styles.display === 'none' || styles.flexGrow !== '0') continue;
                const marginLeft = parseFloat(styles.marginLeft || '0') || 0;
                const marginRight = parseFloat(styles.marginRight || '0') || 0;
                const intrinsicWidth = Math.max(child.scrollWidth, child.getBoundingClientRect().width);
                requiredWidth += intrinsicWidth + marginLeft + marginRight;
            }
            const horizontalPadding = (parseFloat(navbarStyles.paddingLeft || '0') || 0)
                + (parseFloat(navbarStyles.paddingRight || '0') || 0);
            return {
                requiredWidth: requiredWidth + horizontalPadding,
                availableWidth: desktopNavbar.clientWidth
            };
        });
    }

    function shouldUseMobileNavbar() {
        const desktopNavbar = document.getElementById('navbar-desktop');
        const mobileBreakpoint = window.matchMedia('(max-width: 1000px)').matches;
        if (!desktopNavbar) return mobileBreakpoint;
        if (mobileBreakpoint) return true;
        const { requiredWidth, availableWidth } = getDesktopWidthMetrics(desktopNavbar);
        const collapseBuffer = 24;
        const expandBuffer = 24;
        if (exports.currentNavbarMode === 'mobile') {
            return requiredWidth + expandBuffer > availableWidth;
        }
        return requiredWidth + collapseBuffer > availableWidth;
    }

    function updateNavbarMode(trigger = 'js') {
        const desktopNavbar = document.getElementById('navbar-desktop');
        const mobileNavbar = document.getElementById('navbar-mobile');
        const useMobileNavbar = shouldUseMobileNavbar();
        if (!desktopNavbar || !mobileNavbar) {
            console.log(`[Navbar] updateNavbarMode skipped (missing DOM nodes) [trigger=${trigger}]`);
            return;
        }
        document.documentElement.classList.toggle('force-mobile-navbar', useMobileNavbar);
        exports.currentNavbarMode = useMobileNavbar ? 'mobile' : 'desktop';
        console.log(`[Navbar] updateNavbarMode [trigger=${trigger}] → mode=${exports.currentNavbarMode}`);
        if (useMobileNavbar) {
            desktopNavbar.style.setProperty('display', 'none', 'important');
            mobileNavbar.style.setProperty('display', 'grid', 'important');
            return;
        }
        desktopNavbar.style.removeProperty('display');
        mobileNavbar.style.removeProperty('display');
    }

    function queueNavbarModeUpdate(trigger = 'resize') {
        if (exports.navbarModeUpdateQueued) return;
        exports.navbarModeUpdateQueued = true;
        window.requestAnimationFrame(() => {
            exports.navbarModeUpdateQueued = false;
            updateNavbarMode(trigger);
        });
        console.log(`[Navbar] queueNavbarModeUpdate [trigger=${trigger}]`);
    }

    exports.updateNavbarMode = updateNavbarMode;
    exports.queueNavbarModeUpdate = queueNavbarModeUpdate;
    exports.tryUpdateNavbarModeIfReady = function(trigger) {
        const desktopNavbar = document.getElementById('navbar-desktop');
        const mobileNavbar = document.getElementById('navbar-mobile');
        if (desktopNavbar && mobileNavbar && desktopNavbar.offsetParent !== null) {
            exports.updateNavbarMode(trigger + '-poll');
            return true;
        }
        return false;
    };
    exports.startNavbarMutationObserver = function() {
        if (window.__navbarMutationObserver) return;
        const desktopNavbar = document.getElementById('navbar-desktop');
        if (!desktopNavbar) return;
        const observer = new MutationObserver(() => {
            window.weddingNavbar.updateNavbarMode('mutation');
        });
        observer.observe(desktopNavbar, { childList: true, subtree: true, attributes: true });
        window.__navbarMutationObserver = observer;
        // Also observe mobile navbar for completeness
        const mobileNavbar = document.getElementById('navbar-mobile');
        if (mobileNavbar) {
            observer.observe(mobileNavbar, { childList: true, subtree: true, attributes: true });
        }
    };

    window.addEventListener('resize', () => queueNavbarModeUpdate('resize'));
    window.addEventListener('load', () => updateNavbarMode('load'));
    if (document.fonts && document.fonts.ready) {
        document.fonts.ready.then(() => updateNavbarMode('fonts'));
    }
})(window.weddingNavbar);
