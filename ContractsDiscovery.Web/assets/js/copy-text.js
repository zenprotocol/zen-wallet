(function() {

  'use strict';

  // click events
  document.body.addEventListener('click', copy, true);

  // event handler
  function copy(e) {

    // find target element
    var
      t = e.target,
      c = t.dataset.copytarget,
      inp = (c ? document.querySelector(c) : null);

    // is element selectable?
    if (inp && inp.select) {

      // select text
      inp.select();

      try {
        // copy text
        document.execCommand('copy');
        t.innerHTML = 'Copied'
        setTimeout(function () {
          t.innerHTML = 'Copy'
        }, 2000);
      }
      catch (err) {
        alert('please press Ctrl/Cmd+C to copy');
      }

    }

  }

})();


window.onscroll = function() {
    var nav = document.getElementById('navbar');
    if ( window.pageYOffset > 20 ) {
        nav.classList.add("with-border");
    } else {
        nav.classList.remove("with-border");
    }
}
