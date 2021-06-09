// <media-items-table> component
Vue.component('media-items-table', {
    template: `
        <table class="table media-items-table m-0">
            <thead>
                <tr class="header-row">
                    <th scope="col" class="thumbnail-column">{{ T.imageHeader }}</th>
                    <th scope="col" v-on:click="changeSort('name')">
                       {{ T.nameHeader }}
                         <sort-indicator colname="name" :selectedcolname="sortBy" :asc="sortAsc"></sort-indicator>
                    </th>
                    <th scope="col" v-on:click="changeSort('lastModify')"> 
                       {{ T.lastModifyHeader }} 
                         <sort-indicator colname="lastModify" :selectedcolname="sortBy" :asc="sortAsc"></sort-indicator> 
                    </th> 
                    <th scope="col" v-on:click="changeSort('size')">
                        <span class="optional-col">
                            {{ T.sizeHeader }}
                         <sort-indicator colname="size" :selectedcolname="sortBy" :asc="sortAsc"></sort-indicator>
                        </span>
                    </th>
                    <th scope="col" v-on:click="changeSort('mime')">
                        <span class="optional-col">
                           {{ T.typeHeader }}
                         <sort-indicator colname="mime" :selectedcolname="sortBy" :asc="sortAsc"></sort-indicator>
                        </span>
                    </th>
                </tr>
            </thead>
            <tbody>
                    <tr v-for="media in filteredMediaItems"
                          class="media-item"
                          :class="{selected: isMediaSelected(media)}"
                          v-on:click.stop="toggleSelectionOfMedia(media)"
                          draggable="true" v-on:dragstart="dragStart(media, $event)"
                          :key="media.name">
                             <td class="thumbnail-column">
                                <div class="img-wrapper">
                                    <img v-if="media.mime.startsWith('image')" draggable="false" :src="buildMediaUrl(media.url, thumbSize)" />
                                    <i v-else class="fa fa-file-o fa-lg" :data-mime="media.mime"></i>
                                </div>
                            </td>
                            <td>
                                <div class="media-name-cell">
                                   <span class="break-word"> {{ media.name }} </span>
                                    <div class="buttons-container">
                                        <a href="javascript:;" class="btn btn-link btn-sm me-1 edit-button" v-on:click.stop="renameMedia(media)"> {{ T.editButton }} </a >
                                        <a href="javascript:;" class="btn btn-link btn-sm delete-button" v-on:click.stop="deleteMedia(media)"> {{ T.deleteButton }} </a>
                                        <a :href="media.url" target="_blank" class="btn btn-link btn-sm view-button"> {{ T.viewButton }} </a>
                                    </div>
                                </div>
                            </td>
                            <td>
                                <div class="text-col"> {{ printDateTime(media.lastModify) }} </div>
                            </td>
                            <td>
                                <div class="text-col optional-col"> {{ isNaN(media.size)? 0 : Math.round(media.size / 1024) }} KB</div>
                            </td>
                            <td>
                                <div class="text-col optional-col">{{ media.mime }}</div>
                            </td>
                   </tr>
            </tbody>
        </table>
        `,
    data: function () {
        return {
            T: {}
        }
    },
    props: {
        sortBy: String,
        sortAsc: Boolean,
        filteredMediaItems: Array,
        selectedMedias: Array,
        thumbSize: Number
    },
    created: function () {
        var self = this;
        self.T.imageHeader = $('#t-image-header').val();
        self.T.nameHeader = $('#t-name-header').val();
        self.T.lastModifyHeader = $('#t-lastModify-header').val();
        self.T.sizeHeader = $('#t-size-header').val();
        self.T.typeHeader = $('#t-type-header').val();
        self.T.editButton = $('#t-edit-button').val();
        self.T.deleteButton = $('#t-delete-button').val();
        self.T.viewButton = $('#t-view-button').val();
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
        changeSort: function (newSort) {
            bus.$emit('sortChangeRequested', newSort);
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
        },
        printDateTime: function (datemillis){
            var d = new Date(datemillis);
            return d.toLocaleString();            
        }
    }
});
