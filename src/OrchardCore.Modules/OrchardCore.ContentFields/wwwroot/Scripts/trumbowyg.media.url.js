/*
** NOTE: This file is generated by Gulp and should not be edited directly!
** Any changes made directly to this file will be overwritten next time its asset group is processed by Gulp.
*/

(function ($) {
  'use strict';

  $.extend(true, $.trumbowyg, {
    langs: {
      en: {
        insertImage: 'Insert Media'
      }
    },
    plugins: {
      insertImage: {
        init: function init(trumbowyg) {
          var btnDef = {
            fn: function fn() {
              trumbowyg.saveRange();
              $("#mediaApp").detach().appendTo('#mediaModalHtmlField .modal-body');
              $("#mediaApp").show();
              mediaApp.selectedMedias = [];
              var modal = $('#mediaModalHtmlField').modal();
              $('#mediaHtmlFieldSelectButton').on('click', function (v) {
                //avoid multiple image insert
                trumbowyg.restoreRange();
                trumbowyg.range.deleteContents();
                $(window).trigger('scroll');

                for (i = 0; i < mediaApp.selectedMedias.length; i++) {
                  var img = document.createElement("img");
                  img.src = mediaApp.selectedMedias[i].url;
                  img.alt = mediaApp.selectedMedias[i].name;
                  trumbowyg.range.insertNode(img);
                }

                trumbowyg.$c.trigger('tbwchange');
                trumbowyg.$c.focus();
                $('#mediaModalHtmlField').modal('hide');
                return true;
              });
            }
          };
          trumbowyg.addBtnDef('insertImage', btnDef);
        }
      }
    }
  });
})(jQuery);