// <media-items-grid> component
Vue.component('media-items-grid', {
    template: `
        <ol class="row media-items-grid">
                <li v-for="media in filteredMediaItems"
                    :key="media.name" 
                    class="media-item media-container-main-list-item card"
                    :style="{width: thumbSize + 2 + 'px'}"
                    :class="{selected: isMediaSelected(media)}"
                    v-on:click.stop="toggleSelectionOfMedia(media)"
                    draggable="true" v-on:dragstart="dragStart(media, $event)">
                    <div class="thumb-container" :style="{height: thumbSize +'px'}">
                        <img v-if="media.mime.startsWith('image')"
                                :src="buildMediaUrl(media.url, thumbSize)"
                                :data-mime="media.mime"
                                :style="{maxHeight: thumbSize +'px', maxWidth: thumbSize +'px'}" />
                        <i v-else class="fa fa-file-o fa-lg" :data-mime="media.mime"></i>
                    </div>
                <div class="media-container-main-item-title card-body">
                        <a href="javascript:;" class="btn btn-light btn-sm float-end inline-media-button edit-button" v-on:click.stop="renameMedia(media)"><i class="fa fa-edit"></i></a>
                        <a href="javascript:;" class="btn btn-light btn-sm float-end inline-media-button delete-button" v-on:click.stop="deleteMedia(media)"><i class="fa fa-trash"></i></a>
                        <a :href="media.url" target="_blank" class="btn btn-light btn-sm float-end inline-media-button view-button""><i class="fa fa-download"></i></a>
                        <span class="media-filename card-text small" :title="media.name">{{ media.name }}</span>
                    </div>
                 </li>
        </ol>
        `,
    data: function () {
        return {
            T: {}
        }
    },
    props: {
        filteredMediaItems: Array,
        selectedMedias: Array,
        thumbSize: Number
    },
    created: function () {
        var self = this;
        // retrieving localized strings from view
        self.T.editButton = $('#t-edit-button').val();
        self.T.deleteButton = $('#t-delete-button').val();
    },
    methods: {
        isMediaSelected: function (media) {
            var result = this.selectedMedias.some(function (element, index, array) {
                return element.url.toLowerCase() === media.url.toLowerCase();
            });
            return result;
        },
        buildMediaUrl: function (url, thumbSize) {
            return url + (url.indexOf('?') == -1 ? '?' : '&') + 'width=' + thumbSize + '&height=' + thumbSize;
        },
        toggleSelectionOfMedia: function (media) {
            bus.$emit('mediaToggleRequested', media);
        },
        renameMedia: function (media) {
            bus.$emit('renameMediaRequested', media);            
        },
        deleteMedia: function (media) {
            bus.$emit('deleteMediaRequested', media);
        },
        dragStart: function (media, e) {
            bus.$emit('mediaDragStartRequested', media, e);
        }
    }
});
