var fs = require("graceful-fs"),
    glob = require("glob"),
    path = require("path-posix"),
    merge = require("merge-stream"),
    gulp = require("gulp"),
    gulpif = require("gulp-if"),
    print = require("gulp-print"),
    debug = require("gulp-debug"),
    newer = require("gulp-newer"),
    plumber = require("gulp-plumber"),
    sourcemaps = require("gulp-sourcemaps"),
    less = require("gulp-less"),
    scss = require("gulp-sass"),
    minify = require("gulp-minifier"),
    typescript = require("gulp-typescript"),
    terser = require("gulp-terser"),
    rename = require("gulp-rename"),
    concat = require("gulp-concat"),
    header = require("gulp-header"),
    eol = require("gulp-eol"),
    util = require("gulp-util"),
    postcss = require("gulp-postcss"),
    rtl = require("postcss-rtl"),
    babel = require('gulp-babel'),
    blogtheme = require('./src/OrchardCore.Themes/TheBlogTheme/wwwroot/gulpfile');
    comingsoontheme = require('./src/OrchardCore.Themes/TheComingSoonTheme/wwwroot/gulpfile');

// For compat with older versions of Node.js.
require("es6-promise").polyfill();

// To suppress memory leak warning from gulp.watch().
require("events").EventEmitter.prototype._maxListeners = 100;

/*
 ** GULP TASKS
 */

// Incremental build (each asset group is built only if one or more inputs are newer than the output).
gulp.task("build-assets", function () {
    var assetGroupTasks = getAssetGroups().map(function (assetGroup) {
        var doRebuild = false;
        return createAssetGroupTask(assetGroup, doRebuild);
    });
    return merge(assetGroupTasks);
});

// Full rebuild (all assets groups are built regardless of timestamps).
gulp.task("rebuild-assets", function () {
    var assetGroupTasks = getAssetGroups().map(function (assetGroup) {
        var doRebuild = true;
        return createAssetGroupTask(assetGroup, doRebuild);
    });
    return merge(assetGroupTasks);
});

function watchAssetGroup(assetGroup, assetManifestPath) {
    resolveAssetGroupPaths(assetGroup, assetManifestPath);
    // watch the input and watchPaths
    var watchPaths = assetGroup.inputPaths.concat(assetGroup.watchPaths);

    var inputWatcher = gulp.watch(watchPaths);
    inputWatcher.on("change", function (watchedPath) {
        var isConcat =
            path.basename(
                assetGroup.outputFileName,
                path.extname(assetGroup.outputFileName)
            ) !== "@";
        if (isConcat)
            console.log(
                "Asset file '" +
                watchedPath +
                "' was changed, rebuilding asset group with output '" +
                assetGroup.outputPath +
                "'."
            );
        else
            console.log(
                "Asset file '" +
                watchedPath +
                "' was changed, rebuilding asset group."
            );
        var doRebuild = true;
        createAssetGroupTask(assetGroup, doRebuild);
    });
    createAssetGroupTask(assetGroup, false);
    return inputWatcher;
}

function requireUncached(module) {
    delete require.cache[require.resolve(module)];
    return require(module);
}

// Continuous watch (each asset group is built whenever one of its inputs changes).
gulp.task("watch", function () {
    getAssetsJsonPaths().forEach(function (assetManifestPath) {
        // load Assets.json file
        var assetManifest = require("./" + assetManifestPath);
        var groupWatchers = assetManifest.map(function (assetGroup) {
            return watchAssetGroup(assetGroup, assetManifestPath);
        });

        gulp.watch(assetManifestPath).on("change", function (watchedPath) {
            console.log(
                "Asset manifest file '" +
                watchedPath +
                "' was changed, restarting watcher."
            );

            // close existing watchers for the manifest
            groupWatchers.forEach(watcher => watcher.close());
            // reload asset manifest and re-create watchers
            assetManifest = requireUncached("./" + assetManifestPath);
            groupWatchers = assetManifest.map(function (assetGroup) {
                return watchAssetGroup(assetGroup, assetManifestPath);
            });
            console.log(
                "Asset manifest file '" +
                watchedPath +
                "' was sucessfully reloaded"
            );
        });
    });
    console.log("Waiting for file changes");
});

gulp.task("default", gulp.series(["build"]));

gulp.task('help', function() {
    util.log(`
  Usage: gulp [TASK]
  Tasks:
      build     Incremental build (each asset group is built only if one or more inputs are newer than the output).
      rebuild   Full rebuild (all assets groups are built regardless of timestamps).
      watch     Continuous watch (each asset group is built whenever one of its inputs changes).
    `);
});

gulp.task("build-blogtheme", function(done){
	return buildBlogTheme(done);
});

gulp.task("build-comingsoontheme", function(done){
	return buildComingSoonTheme(done);
});

gulp.task("build-themes", function(done){
    buildBlogTheme (() => buildComingSoonTheme(done));
});

gulp.task( 'build',  gulp.series([ 'build-assets', 'build-themes' ]) );
gulp.task( 'rebuild',  gulp.series([ 'rebuild-assets', 'build-themes' ]) );
gulp.task( 'default',  gulp.series([ 'build' ]) );

/*
** Build Themes
*/

function buildBlogTheme(done){
    var cwd = process.cwd();      
    process.chdir('./src/OrchardCore.Themes/TheBlogTheme/wwwroot');    
	blogtheme.build( ()=> {
        process.chdir(cwd);
        done();
    });
}

function buildComingSoonTheme(done){
    var cwd = process.cwd();      
    process.chdir('./src/OrchardCore.Themes/TheComingSoonTheme/wwwroot');    
	comingsoontheme.build( ()=> {
        process.chdir(cwd);
        done();
    });
}

/*
 ** ASSET GROUPS
 */

function getAllAssetGroups() {
    var assetGroups = [];
    getAssetsJsonPaths().forEach(function (assetManifestPath) {
        var assetManifest = require("./" + assetManifestPath);
        assetManifest.forEach(function (assetGroup) {
            resolveAssetGroupPaths(assetGroup, assetManifestPath);
            assetGroups.push(assetGroup);
        });
    });
    return assetGroups;
}

function getAssetsJsonPaths() {
    return glob.sync("./src/OrchardCore.{Modules,Themes}/*/Assets.json", {});
}

function resolveAssetGroupPaths(assetGroup, assetManifestPath) {
    assetGroup.manifestPath = assetManifestPath;
    assetGroup.basePath = path.dirname(assetManifestPath);
    assetGroup.inputPaths = assetGroup.inputs.map(function (inputPath) {
        return path
            .resolve(path.join(assetGroup.basePath, inputPath))
            .replace(/\\/g, "/");
    });
    assetGroup.watchPaths = [];
    if (!!assetGroup.watch) {
        assetGroup.watchPaths = assetGroup.watch.map(function (watchPath) {
            return path
                .resolve(path.join(assetGroup.basePath, watchPath))
                .replace(/\\/g, "/");
        });
    }
    assetGroup.outputPath = path
        .resolve(path.join(assetGroup.basePath, assetGroup.output))
        .replace(/\\/g, "/");
    assetGroup.outputDir = path.dirname(assetGroup.outputPath);
    assetGroup.outputFileName = path.basename(assetGroup.output);
    // Uncomment to copy assets to wwwroot
    //assetGroup.webroot = path.join("./src/OrchardCore.Cms.Web/wwwroot/", path.basename(assetGroup.basePath), path.dirname(assetGroup.output));
}

function createAssetGroupTask(assetGroup, doRebuild) {
    var outputExt = path.extname(assetGroup.output).toLowerCase();
    var doConcat = path.basename(assetGroup.outputFileName, outputExt) !== "@";
    if (doConcat && !doRebuild) {
        // Force a rebuild of this asset group is the asset manifest file itself is newer than the output.
        var assetManifestStats = fs.statSync(assetGroup.manifestPath);
        var outputStats = fs.existsSync(assetGroup.outputPath)
            ? fs.statSync(assetGroup.outputPath)
            : null;
        doRebuild =
            !outputStats || assetManifestStats.mtime > outputStats.mtime;
    }

    if (assetGroup.copy === true) {
        return buildCopyPipeline(assetGroup, doRebuild);
    } else {
        switch (outputExt) {
            case ".css":
                return buildCssPipeline(assetGroup, doConcat, doRebuild);
            case ".js":
                return buildJsPipeline(assetGroup, doConcat, doRebuild);
        }
    }
}

/*
 ** PROCESSING PIPELINES
 */

function buildCssPipeline(assetGroup, doConcat, doRebuild) {
    assetGroup.inputPaths.forEach(function (inputPath) {
        var ext = path.extname(inputPath).toLowerCase();
        if (ext !== ".scss" && ext !== ".less" && ext !== ".css")
            throw "Input file '" +
            inputPath +
            "' is not of a valid type for output file '" +
            assetGroup.outputPath +
            "'.";
    });
    var generateSourceMaps = assetGroup.hasOwnProperty("generateSourceMaps")
        ? assetGroup.generateSourceMaps
        : true;
    var generateRTL = assetGroup.hasOwnProperty("generateRTL")
        ? assetGroup.generateRTL
        : false;
    var containsLessOrScss = assetGroup.inputPaths.some(function (inputPath) {
        var ext = path.extname(inputPath).toLowerCase();
        return ext === ".less" || ext === ".scss";
    });
    // Source maps are useless if neither concatenating nor transforming.
    if ((!doConcat || assetGroup.inputPaths.length < 2) && !containsLessOrScss)
        generateSourceMaps = false;

    console.log(assetGroup.inputPaths);
    var minifiedStream = gulp
        .src(assetGroup.inputPaths) // Minified output, source mapping completely disabled.
        .pipe(
            gulpif(
                !doRebuild,
                gulpif(
                    doConcat,
                    newer(assetGroup.outputPath),
                    newer({
                        dest: assetGroup.outputDir,
                        ext: ".css"
                    })
                )
            )
        )
        .pipe(plumber())
        .pipe(gulpif("*.less", less()))
        .pipe(
            gulpif(
                "*.scss",
                scss({
                    precision: 10
                })
            )
        )
        .pipe(gulpif(doConcat, concat(assetGroup.outputFileName)))
        .pipe(gulpif(generateRTL, postcss([rtl()])))
        .pipe(
            minify({
                minify: true,
                minifyHTML: {
                    collapseWhitespace: true,
                    conservativeCollapse: true
                },
                minifyJS: {
                    sourceMap: true
                },
                minifyCSS: true
            })
        )
        .pipe(
            rename({
                suffix: ".min"
            })
        )
        .pipe(eol())
        .pipe(gulp.dest(assetGroup.outputDir));
    // Uncomment to copy assets to wwwroot
    //.pipe(gulp.dest(assetGroup.webroot));
    var devStream = gulp
        .src(assetGroup.inputPaths) // Non-minified output, with source mapping
        .pipe(
            gulpif(
                !doRebuild,
                gulpif(
                    doConcat,
                    newer(assetGroup.outputPath),
                    newer({
                        dest: assetGroup.outputDir,
                        ext: ".css"
                    })
                )
            )
        )
        .pipe(plumber())
        .pipe(gulpif(generateSourceMaps, sourcemaps.init()))
        .pipe(gulpif("*.less", less()))
        .pipe(
            gulpif(
                "*.scss",
                scss({
                    precision: 10
                })
            )
        )
        .pipe(gulpif(doConcat, concat(assetGroup.outputFileName)))
        .pipe(
            header(
                "/*\n" +
                "** NOTE: This file is generated by Gulp and should not be edited directly!\n" +
                "** Any changes made directly to this file will be overwritten next time its asset group is processed by Gulp.\n" +
                "*/\n\n"
            )
        )
        .pipe(gulpif(generateRTL, postcss([rtl()])))
        .pipe(gulpif(generateSourceMaps, sourcemaps.write()))
        .pipe(eol())
        .pipe(gulp.dest(assetGroup.outputDir));
    // Uncomment to copy assets to wwwroot
    //.pipe(gulp.dest(assetGroup.webroot));
    return merge([minifiedStream, devStream]);
}

function buildJsPipeline(assetGroup, doConcat, doRebuild) {
    assetGroup.inputPaths.forEach(function (inputPath) {
        var ext = path.extname(inputPath).toLowerCase();
        if (ext !== ".ts" && ext !== ".js")
            throw "Input file '" +
            inputPath +
            "' is not of a valid type for output file '" +
            assetGroup.outputPath +
            "'.";
    });
    var generateSourceMaps = assetGroup.hasOwnProperty("generateSourceMaps")
        ? assetGroup.generateSourceMaps
        : true;
    // Source maps are useless if neither concatenating nor transforming.
    if (
        (!doConcat || assetGroup.inputPaths.length < 2) &&
        !assetGroup.inputPaths.some(function (inputPath) {
            return path.extname(inputPath).toLowerCase() === ".ts";
        })
    )
        generateSourceMaps = false;

    console.log(assetGroup.inputPaths);
    return (
        gulp
            .src(assetGroup.inputPaths)
            .pipe(
                gulpif(
                    !doRebuild,
                    gulpif(
                        doConcat,
                        newer(assetGroup.outputPath),
                        newer({
                            dest: assetGroup.outputDir,
                            ext: ".js"
                        })
                    )
                )
            )
            .pipe(plumber())
            .pipe(gulpif(generateSourceMaps, sourcemaps.init()))
            .pipe(
                gulpif(
                    "*.ts",
                    typescript({
                        declaration: false,
                        noImplicitAny: true,
                        noEmitOnError: true,
                        lib: ["dom", "es5", "scripthost", "es2015.iterable"],
                        target: "es5"
                    })
                )
            )
            .pipe(
                babel({
                    presets: [
                        [
                            "@babel/preset-env",
                            {
                                modules: false
                            },
                            "@babel/preset-flow"
                        ]
                    ]
                })
            )
            .pipe(gulpif(doConcat, concat(assetGroup.outputFileName)))
            .pipe(
                header(
                    "/*\n" +
                    "** NOTE: This file is generated by Gulp and should not be edited directly!\n" +
                    "** Any changes made directly to this file will be overwritten next time its asset group is processed by Gulp.\n" +
                    "*/\n\n"
                )
            )
            .pipe(gulpif(generateSourceMaps, sourcemaps.write()))
            .pipe(gulp.dest(assetGroup.outputDir))
            // Uncomment to copy assets to wwwroot
            //.pipe(gulp.dest(assetGroup.webroot))
            .pipe(terser())
            .pipe(
                rename({
                    suffix: ".min"
                })
            )
            .pipe(eol())
            .pipe(gulp.dest(assetGroup.outputDir))
    );
    // Uncomment to copy assets to wwwroot
    //.pipe(gulp.dest(assetGroup.webroot));
}

function buildCopyPipeline(assetGroup, doRebuild) {
    var stream = gulp.src(assetGroup.inputPaths);

    if (!doRebuild) {
        stream = stream.pipe(newer(assetGroup.outputDir));
    }

    var renameFile = assetGroup.outputFileName != "@";

    stream = stream
        .pipe(gulpif(renameFile, rename(assetGroup.outputFileName)))
        .pipe(gulp.dest(assetGroup.outputDir));
    // Uncomment to copy assets to wwwroot
    //.pipe(gulp.dest(assetGroup.webroot));

    return stream;
}
