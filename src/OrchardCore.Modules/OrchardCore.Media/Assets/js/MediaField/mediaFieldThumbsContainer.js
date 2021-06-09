// <media-field-thumbs-container> component 
// different media field editors share this component to present the thumbs.
Vue.component('mediaFieldThumbsContainer', {
    template: '\
       <div :id="idPrefix + \'_mediaContainerMain\'" v-cloak>\
         <div v-if="mediaItems.length < 1" class="card text-center">\
             <div class= "card-body" >\
                <span class="hint">{{T.noImages}}</span>\
             </div>\
         </div>\
         <draggable :list="mediaItems" tag="ol" class="row media-items-grid" >\
            <li v-for="media in mediaItems"\
                :key="media.vuekey" \
                class="media-container-main-list-item card"\
                :style="{width: thumbSize + 2 + \'px\'}"\
                :class="{selected: selectedMedia == media}"\
                v-on:click="selectMedia(media)" v-if="!media.isRemoved">\
                    <div v-if="media.mediaPath!== \'not-found\'">\
                        <div class="thumb-container" :style="{height: thumbSize + \'px\'}" >\
                            <img v-if="media.mime.startsWith(\'image\')" \
                            :src="buildMediaUrl(media.url, thumbSize)" \
                            :data-mime="media.mime"\
                            :style="{maxHeight: thumbSize + \'px\' , maxWidth: thumbSize + \'px\'}"/>\
                            <i v-else class="fa fa-file-o fa-lg" :data-mime="media.mime"></i>\
                         </div>\
                         <div class="media-container-main-item-title card-body">\
                                <a href="javascript:;" class="btn btn-light btn-sm float-end inline-media-button delete-button"\
                                    v-on:click.stop="selectAndDeleteMedia(media)"><i class="fa fa-trash"></i></a>\
                                <a :href="media.url" target="_blank" class="btn btn-light btn-sm float-end inline-media-button view-button""><i class="fa fa-download"></i></a> \
                                <span class="media-filename card-text small" :title="media.mediaPath">{{ media.isNew ? media.name.substr(36) : media.name }}</span>\
                         </div>\
                    </div>\
                    <div v-else>\
                        <div class="thumb-container flex-column" :style="{height: thumbSize + \'px\'}">\
                            <i class="fa fa-ban text-danger d-block"></i>\
                            <span class="text-danger small d-block">{{ T.mediaNotFound }}</span>\
                            <span class="text-danger small d-block text-center">{{ T.discardWarning }}</span>\
                        </div>\
                        <div class="media-container-main-item-title card-body">\
                            <a href="javascript:;" class="btn btn-light btn-sm float-end inline-media-button delete-button"\
                                v-on:click.stop="selectAndDeleteMedia(media)"><i class="fa fa-trash"></i></a>\
                            <span class="media-filename card-text small text-danger" :title="media.name">{{ media.name }}</span>\
                        </div>\
                   </div>\
            </li>\
         </draggable>\
       </div>\
    ',
    data: function () {
        return {
            T: {}
        };
    },
    props: {
        mediaItems: Array,
        selectedMedia: Object,
        thumbSize: Number,
        idPrefix: String
    },
    created: function () {

        var self = this;

        // retrieving localized strings from view
        self.T.mediaNotFound = $('#t-media-not-found').val();
        self.T.discardWarning = $('#t-discard-warning').val();
        self.T.noImages = $('#t-no-images').val();
    },
    methods: {
        selectAndDeleteMedia: function (media) {            
            this.$parent.$emit('selectAndDeleteMediaRequested', media);
        },
        selectMedia: function (media) {
            this.$parent.$emit('selectMediaRequested', media);
        },
        buildMediaUrl: function (url, thumbSize) {
            return url + (url.indexOf('?') == -1 ? '?' : '&') + 'width=' + thumbSize + '&height=' + thumbSize;
        }
    }
});
