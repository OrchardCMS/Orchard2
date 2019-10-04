/*
** NOTE: This file is generated by Gulp and should not be edited directly!
** Any changes made directly to this file will be overwritten next time its asset group is processed by Gulp.
*/

"use strict";

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
                var mediaBodyContent = "";

                for (i = 0; i < mediaApp.selectedMedias.length; i++) {
                  mediaBodyContent += ' {{ "' + mediaApp.selectedMedias[i].mediaPath + '" | asset_url | img_tag }}';
                }

                var node = document.createTextNode(mediaBodyContent);
                trumbowyg.restoreRange();
                trumbowyg.range.deleteContents();
                trumbowyg.range.insertNode(node);
                trumbowyg.syncTextarea();
                $(document).trigger('contentpreview:render');
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