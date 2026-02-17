document.addEventListener('DOMContentLoaded', function() {
    const navToggle = document.getElementById('nav-toggle');
    const navMenu = document.getElementById('nav-menu');
    const body = document.body;

    if (navToggle && navMenu) {
        function toggleMenu() {
            navToggle.classList.toggle('active');
            navMenu.classList.toggle('active');
            
            if (navMenu.classList.contains('active')) {
                body.style.overflow = 'hidden';
            } else {
                body.style.overflow = '';
            }
        }

        navToggle.addEventListener('click', (e) => {
            e.stopPropagation();
            toggleMenu();
        });

        document.querySelectorAll('.nav-link').forEach(link => {
            link.addEventListener('click', () => {
                navToggle.classList.remove('active');
                navMenu.classList.remove('active');
                body.style.overflow = '';
            });
        });

        document.addEventListener('click', (event) => {
            const isClickInsideMenu = navMenu.contains(event.target);
            const isClickOnToggle = navToggle.contains(event.target);
            
            if (navMenu.classList.contains('active') && !isClickInsideMenu && !isClickOnToggle) {
                navToggle.classList.remove('active');
                navMenu.classList.remove('active');
                body.style.overflow = '';
            }
        });

        navMenu.addEventListener('click', (e) => {
            e.stopPropagation();
        });

        window.addEventListener('resize', () => {
            if (window.innerWidth > 768 && navMenu.classList.contains('active')) {
                navToggle.classList.remove('active');
                navMenu.classList.remove('active');
                body.style.overflow = '';
            }
        });
    }

    const fadeElements = document.querySelectorAll('.fade-in');

    function isElementInViewport(el) {
        const rect = el.getBoundingClientRect();
        const windowHeight = window.innerHeight || document.documentElement.clientHeight;
        
        return (
            rect.top <= windowHeight * 0.85 &&
            rect.bottom >= 0
        );
    }

    function handleScrollAnimation() {
        fadeElements.forEach(element => {
            if (isElementInViewport(element)) {
                element.classList.add('visible');
            }
        });
    }

    window.addEventListener('scroll', handleScrollAnimation);
    handleScrollAnimation();

    const scrollButton = document.getElementById('scrollToTop');

    if (scrollButton) {
        window.addEventListener('scroll', () => {
            if (window.scrollY > 300) {
                scrollButton.style.display = 'block';
            } else {
                scrollButton.style.display = 'none';
            }
        });

        scrollButton.addEventListener('click', () => {
            window.scrollTo({
                top: 0,
                behavior: 'smooth'
            });
        });
    }

    const gallery = document.querySelector('.gallery');
    if (gallery) {
        gallery.addEventListener('mouseenter', () => {
            gallery.style.animationPlayState = 'paused';
        });
        
        gallery.addEventListener('mouseleave', () => {
            gallery.style.animationPlayState = 'running';
        });
    }
});