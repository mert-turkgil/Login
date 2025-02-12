// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
document.addEventListener("DOMContentLoaded", function () {
    const lazyCards = document.querySelectorAll('.lazy-card');
  
    const observerOptions = {
      root: null,
      rootMargin: '0px',
      threshold: 0.2  // Adjust threshold as needed
    };
  
    const onIntersection = (entries, observer) => {
      entries.forEach(entry => {
        if (entry.isIntersecting) {
          // Add animate.css classes
          entry.target.classList.add('animate__animated', 'animate__fadeInUp');
          // Unobserve once the animation is triggered
          observer.unobserve(entry.target);
        }
      });
    };
  
    const observer = new IntersectionObserver(onIntersection, observerOptions);
    lazyCards.forEach(card => observer.observe(card));
  });
  