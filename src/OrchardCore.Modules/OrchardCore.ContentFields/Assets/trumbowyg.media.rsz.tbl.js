(function ($) {
    'use strict';

    var defaultOptions = {
        minSize: 32,
        step: 4,
    };

    $.extend(true, $.trumbowyg, {
        plugins: {
            insertImage: {
                init: function (trumbowyg) {
                    var btnDef = {
                        fn: function () {
                            var t = trumbowyg;
                            t.saveRange();
                            $("#mediaApp").detach().appendTo('#mediaModalHtmlField .modal-body');
                            $("#mediaApp").show();
                            mediaApp.selectedMedias = [];
                            var modal = $('#mediaModalHtmlField').modal();
                            $('#mediaHtmlFieldSelectButton').on('click', function (v) {
                                //set focus on editor to avoid strange issue on image insertion
                                t.$ed.focus();
                                
                                t.restoreRange();
                                t.range.deleteContents();

                                $(window).trigger('scroll');

                                for (i = 0; i < mediaApp.selectedMedias.length; i++) {
                                    var img = document.createElement("img");
                                    img.src = mediaApp.selectedMedias[i].url; //mediaApp.selectedMedias[i].mediaPath;
                                    img.alt = mediaApp.selectedMedias[i].name;
                                    t.range.insertNode(img);
                                }

                                t.syncCode();

                                t.$c.trigger('tbwchange');
                                

                                $('#mediaModalHtmlField').modal('hide');
                                return true;
                            });
                        }
                    };

                    trumbowyg.addBtnDef('insertImage', btnDef);
                }
            },
            resizimg: { //created from original trumbowyg resizer code adn improved
                init: function (trumbowyg) {

                    //object to interact with canvas
                    var rszwtcanvas = new ResizeWithCanvas();
                    
                    trumbowyg.o.plugins.resizimg = $.extend(true, {},
                        defaultOptions,
                        trumbowyg.o.plugins.resizimg || {},
                        {
                            resizable: {
                                resizeWidth: false,
                                onDragStart: function (ev, $el) {
                                    var opt = trumbowyg.o.plugins.resizimg;
                                    var x = ev.pageX - $el.offset().left;
                                    var y = ev.pageY - $el.offset().top;
                                    if (x < $el.width() - opt.minSize || y < $el.height() - opt.minSize) {
                                        return false;
                                    }
                                },
                                onDrag: function (ev, $el, newWidth, newHeight) {
                                    var opt = trumbowyg.o.plugins.resizimg;
                                    if (newHeight < opt.minSize) {
                                        newHeight = opt.minSize;
                                    }
                                    newHeight -= newHeight % opt.step;
                                    $el.height(newHeight);
                                    return false;
                                },
                                onDragEnd: function () {
                                    //resize update canvas information
                                    rszwtcanvas.refresh();
                                    trumbowyg.syncCode();
                                }
                            }
                        }
                    );

                    function initResizable() {
                        trumbowyg.$ed.find('img')
                            .off('click')
                            .on('click', function (ev) {
                                //if I'm already do a resize, reset it
                                if (rszwtcanvas.isActive()){
                                    rszwtcanvas.reset();
                                }
                                //initialize resize of image
                                rszwtcanvas.setup(this, trumbowyg.o.plugins.resizimg.resizable);
                            })
                    }

                    function preventDefault(ev) {
                        return ev.preventDefault();
                    }

                    function destroyResizable(trumbowyg) {
                        //clean html code
                        trumbowyg.$ed.find('canvas.resizable')
                            .resizable('destroy')
                            .off('mousedown', preventDefault)
                            .removeClass('resizable');

                        rszwtcanvas.reset();

                        trumbowyg.syncCode();
                    }

                    // Init
                    trumbowyg.$c.on('tbwinit', function (ev) {
                        initResizable();

                        //disable resize when click on other items
                        trumbowyg.$ed.on('click', function (ev) {
                            // tell the browser we're handling this event
                            ev.preventDefault();
                            ev.stopPropagation();

                            //check if I've clicked out of canvas or image to reset it
                            if (!($(ev.target).is('img') || ev.target.id === rszwtcanvas.canvasId())) {
                                rszwtcanvas.reset();
                                
                                //save changes
                                trumbowyg.$c.trigger('tbwchange');
                            }
                        });

                        trumbowyg.$ed.on('scroll', function (ev) {
                            rszwtcanvas.reCalcOffset();
                        });
                    });
                    trumbowyg.$c.on('tbwfocus', initResizable);
                    trumbowyg.$c.on('tbwchange', initResizable);
                    trumbowyg.$c.on('tbwresize', function (ev){ rszwtcanvas.reCalcOffset(); });


                    // Destroy
                    trumbowyg.$c.on('tbwblur', function () {
                        destroyResizable(trumbowyg);
                    });
                    trumbowyg.$c.on('tbwclose', function () {
                        destroyResizable(trumbowyg);
                    });
                },
                destroy: function (trumbowyg) {
                    destroyResizable(trumbowyg);
                }
            }
        }
    });
})(jQuery);