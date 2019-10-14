/*
** NOTE: This file is generated by Gulp and should not be edited directly!
** Any changes made directly to this file will be overwritten next time its asset group is processed by Gulp.
*/

function ResizeWithCanvas() {
    //variable to create canvas and save img in resize mode
    this.resizecanvas = document.createElement('canvas');
    this.resizecanvas.id = "tbwresizeme";
    this.ctx = null;
    this.resizeimg = null;

    //PRIVATE FUNCTION
    var cursors = ['default', 'se-resize', 'not-allowed'];
    var currentCursor = 0;
    var stylesFilled = ['rgb(0, 0, 0)', 'rgb(200, 0, 0)'];

    var shapesFilled = [];
    shapesFilled.push({
        points: { x: 0, y: 0, width: 0, height: 0 },
        cursor: 2,
        style: 0
    });
    shapesFilled.push({
        points: { x: 0, y: 0, width: 0, height: 0 },
        cursor: 2,
        style: 0
    });
    shapesFilled.push({
        points: { x: 0, y: 0, width: 0, height: 0 },
        cursor: 2,
        style: 0
    });
    shapesFilled.push({
        points: { x: 0, y: 0, width: 0, height: 0 },
        cursor: 1,
        style: 1
    });

    //calculate offset to change mouse over square in the canvas
    var offsetX, offsetY;
    var reOffset = function (canvas) {
        console.log("reset offset");
        var BB = canvas.getBoundingClientRect();
        offsetX = BB.left;
        offsetY = BB.top;
    };

    var drawRect = function (shapedata, ctx) {
        console.log("draw rect");
        ctx.beginPath();
        ctx.fillStyle = stylesFilled[shapedata.style];
        ctx.rect(shapedata.points.x, shapedata.points.y, shapedata.points.width, shapedata.points.height);
        ctx.fill();
    };

    var updateCanvas = function(canvas, ctx, img, canvasWidth, canvasHeight){
        
        //square in the angle
        shapesFilled[0].points = { x: -10, y: -10, width: 20, height: 20 }
        shapesFilled[1].points = { x: canvasWidth - 10, y: -10, width: 20, height: 20 }
        shapesFilled[2].points = { x: -10, y: canvasHeight - 10, width: 20, height: 20 }
        shapesFilled[3].points = { x: canvasWidth - 10, y: canvasHeight - 10, width: 20, height: 20 }

        for (var i = 0; i < shapesFilled.length; i++) {
            drawRect(shapesFilled[i], ctx);
        }

        //border
        ctx.beginPath();
        ctx.rect(5, 5, canvasWidth - 10, canvasHeight - 10);
        ctx.stroke();

        //image
        ctx.drawImage(img, 10, 10, canvasWidth - 20, canvasHeight - 20);

        //get the offset to change the mouse cursor 
        reOffset(canvas);

        return ctx;
    }

    //PUBLIC FUNCTION
    //necessary to correctly print cursor over square. Called once for instance. unuseful with trumbowyg
    this.init = function(){
        var _this = this;
        window.onscroll=function(ev) { console.log("onscroll offset"); reOffset(_this.resizecanvas); };
        window.onresize=function(ev) { console.log("onresize offset"); reOffset(_this.resizecanvas); }; 
    }

    this.reCalcOffset = function(){
        reOffset(this.resizecanvas);
    }

    this.canvasId = function () {
        return this.resizecanvas.id;
    };

    this.isActive = function () {
        return this.resizeimg !== null;
    };

    //restore image in the HTML of the editor
    this.reset = function () {
        console.log("resize reset");

        if (this.resizeimg !== null) {
            this.resizeimg.width = this.resizecanvas.clientWidth - 20;
            this.resizeimg.height = this.resizecanvas.clientHeight - 20;
            //sostituisce il canvas con l'immagine
            $(this.resizecanvas).replaceWith($(this.resizeimg));

            //reset canvas style
            this.resizecanvas.style = "";
            this.resizeimg = null;
        }
    };

    //setup canvas with points and border to allow the resizing operation
    this.setup = function (img, resizableopt) {
        console.log("resize setup");

        this.resizeimg = img;

        if (this.resizecanvas.getContext) {
            //draw canvas
            this.resizecanvas.width = $(this.resizeimg).width() + 20;
            this.resizecanvas.height = $(this.resizeimg).height() + 20;
            this.ctx = this.resizecanvas.getContext('2d');

            //sostituisce l'immagine con il canvas
            $(this.resizeimg).replaceWith($(this.resizecanvas));

            updateCanvas(this.resizecanvas, this.ctx, this.resizeimg, this.resizecanvas.width, this.resizecanvas.height);

            //enable resize
            $(this.resizecanvas).resizable(resizableopt)
                .on('mousedown', function (ev) { return ev.preventDefault(); });

            var _ctx = this.ctx;
            $(this.resizecanvas)
                .on("mousemove", function (e) {                    
                    mouseX = parseInt(e.clientX - offsetX);
                    mouseY = parseInt(e.clientY - offsetY);

                    // Put your mousemove stuff here
                    var newCursor;
                    for (var i = 0; i < shapesFilled.length; i++) {
                        var s = shapesFilled[i];
                        drawRect(s, _ctx);
                        if (_ctx.isPointInPath(mouseX, mouseY)) {
                            newCursor = s.cursor;
                            break;
                        }
                    }
                    if (!newCursor) {
                        if (currentCursor > 0) {
                            currentCursor = 0;
                            this.style.cursor = cursors[currentCursor];
                        }
                    } else if (!newCursor == currentCursor) {
                        currentCursor = newCursor;
                        this.style.cursor = cursors[currentCursor];
                    }
                });

            return true;
        }

        return false;
    };

    //update the canvas after the resizing
    this.refresh = function(){
        console.log("resize refresh");

        if (this.resizecanvas.getContext) {
            this.resizecanvas.width = this.resizecanvas.clientWidth;
            this.resizecanvas.height = this.resizecanvas.clientHeight;
            //updateCanvas(this.resizecanvas, this.ctx, this.resizeimg, this.resizecanvas.clientWidth, this.resizecanvas.clientHeight);
            updateCanvas(this.resizecanvas, this.ctx, this.resizeimg, this.resizecanvas.width, this.resizecanvas.height);
        }
    };
}
