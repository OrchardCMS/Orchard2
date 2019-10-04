/*
** NOTE: This file is generated by Gulp and should not be edited directly!
** Any changes made directly to this file will be overwritten next time its asset group is processed by Gulp.
*/

"use strict";

///<reference path="../../../Assets/Lib/jquery/typings.d.ts" />
$(function () {
  var generateWorkflowUrl = function generateWorkflowUrl() {
    var workflowTypeId = $('[data-workflow-type-id]').data('workflow-type-id');
    var activityId = $('[data-activity-id]').data('activity-id');
    var tokenLifeSpan = $('#token-lifespan').val();
    var generateUrl = $('[data-generate-url]').data('generate-url') + "?workflowTypeId=".concat(workflowTypeId, "&activityId=").concat(activityId, "&tokenLifeSpan=").concat(tokenLifeSpan);
    var antiforgeryHeaderName = $('[data-antiforgery-header-name]').data('antiforgery-header-name');
    var antiforgeryToken = $('[data-antiforgery-token]').data('antiforgery-token');
    var headers = {};
    headers[antiforgeryHeaderName] = antiforgeryToken;
    $.post({
      url: generateUrl,
      headers: headers
    }).done(function (url) {
      $('#workflow-url-text').val(url);
    });
  };

  $('#generate-url-button').on('click', function (e) {
    generateWorkflowUrl();
  });

  if ($('#workflow-url-text').val() == '') {
    generateWorkflowUrl();
  }
});
//# sourceMappingURL=data:application/json;charset=utf8;base64,eyJ2ZXJzaW9uIjozLCJzb3VyY2VzIjpbIndvcmtmbG93LXVybC1nZW5lcmF0b3IudHMiXSwibmFtZXMiOlsiJCIsImdlbmVyYXRlV29ya2Zsb3dVcmwiLCJ3b3JrZmxvd1R5cGVJZCIsImRhdGEiLCJhY3Rpdml0eUlkIiwidG9rZW5MaWZlU3BhbiIsInZhbCIsImdlbmVyYXRlVXJsIiwiYW50aWZvcmdlcnlIZWFkZXJOYW1lIiwiYW50aWZvcmdlcnlUb2tlbiIsImhlYWRlcnMiLCJwb3N0IiwidXJsIiwiZG9uZSIsIm9uIiwiZSJdLCJtYXBwaW5ncyI6Ijs7Ozs7OztBQUFBO0FBRUFBLENBQUMsQ0FBQyxZQUFNO0FBQ0osTUFBTUMsbUJBQW1CLEdBQUcsU0FBdEJBLG1CQUFzQixHQUFZO0FBQ3BDLFFBQU1DLGNBQXNCLEdBQUdGLENBQUMsQ0FBQyx5QkFBRCxDQUFELENBQTZCRyxJQUE3QixDQUFrQyxrQkFBbEMsQ0FBL0I7QUFDQSxRQUFNQyxVQUFrQixHQUFHSixDQUFDLENBQUMsb0JBQUQsQ0FBRCxDQUF3QkcsSUFBeEIsQ0FBNkIsYUFBN0IsQ0FBM0I7QUFDQSxRQUFJRSxhQUFhLEdBQUdMLENBQUMsQ0FBQyxpQkFBRCxDQUFELENBQXFCTSxHQUFyQixFQUFwQjtBQUNBLFFBQU1DLFdBQW1CLEdBQUdQLENBQUMsQ0FBQyxxQkFBRCxDQUFELENBQXlCRyxJQUF6QixDQUE4QixjQUE5Qiw4QkFBbUVELGNBQW5FLHlCQUFnR0UsVUFBaEcsNEJBQTRIQyxhQUE1SCxDQUE1QjtBQUNBLFFBQU1HLHFCQUE2QixHQUFHUixDQUFDLENBQUMsZ0NBQUQsQ0FBRCxDQUFvQ0csSUFBcEMsQ0FBeUMseUJBQXpDLENBQXRDO0FBQ0EsUUFBTU0sZ0JBQXdCLEdBQUdULENBQUMsQ0FBQywwQkFBRCxDQUFELENBQThCRyxJQUE5QixDQUFtQyxtQkFBbkMsQ0FBakM7QUFDQSxRQUFNTyxPQUFZLEdBQUcsRUFBckI7QUFFQUEsSUFBQUEsT0FBTyxDQUFDRixxQkFBRCxDQUFQLEdBQWlDQyxnQkFBakM7QUFFQVQsSUFBQUEsQ0FBQyxDQUFDVyxJQUFGLENBQU87QUFDSEMsTUFBQUEsR0FBRyxFQUFFTCxXQURGO0FBRUhHLE1BQUFBLE9BQU8sRUFBRUE7QUFGTixLQUFQLEVBR0dHLElBSEgsQ0FHUSxVQUFBRCxHQUFHLEVBQUk7QUFDWFosTUFBQUEsQ0FBQyxDQUFDLG9CQUFELENBQUQsQ0FBd0JNLEdBQXhCLENBQTRCTSxHQUE1QjtBQUNILEtBTEQ7QUFNSCxHQWpCRDs7QUFtQkFaLEVBQUFBLENBQUMsQ0FBQyxzQkFBRCxDQUFELENBQTBCYyxFQUExQixDQUE2QixPQUE3QixFQUFzQyxVQUFBQyxDQUFDLEVBQUk7QUFDdkNkLElBQUFBLG1CQUFtQjtBQUN0QixHQUZEOztBQUlBLE1BQUlELENBQUMsQ0FBQyxvQkFBRCxDQUFELENBQXdCTSxHQUF4QixNQUFpQyxFQUFyQyxFQUF5QztBQUNyQ0wsSUFBQUEsbUJBQW1CO0FBQ3RCO0FBQ0osQ0EzQkEsQ0FBRCIsImZpbGUiOiJvcmNoYXJkLndvcmtmbG93cy1lZGl0b3IuanMiLCJzb3VyY2VzQ29udGVudCI6WyIvLy88cmVmZXJlbmNlIHBhdGg9XCIuLi8uLi8uLi9Bc3NldHMvTGliL2pxdWVyeS90eXBpbmdzLmQudHNcIiAvPlxyXG5cclxuJCgoKSA9PiB7XHJcbiAgICBjb25zdCBnZW5lcmF0ZVdvcmtmbG93VXJsID0gZnVuY3Rpb24gKCkge1xyXG4gICAgICAgIGNvbnN0IHdvcmtmbG93VHlwZUlkOiBzdHJpbmcgPSAkKCdbZGF0YS13b3JrZmxvdy10eXBlLWlkXScpLmRhdGEoJ3dvcmtmbG93LXR5cGUtaWQnKTtcclxuICAgICAgICBjb25zdCBhY3Rpdml0eUlkOiBzdHJpbmcgPSAkKCdbZGF0YS1hY3Rpdml0eS1pZF0nKS5kYXRhKCdhY3Rpdml0eS1pZCcpO1xyXG4gICAgICAgIHZhciB0b2tlbkxpZmVTcGFuID0gJCgnI3Rva2VuLWxpZmVzcGFuJykudmFsKCk7XHJcbiAgICAgICAgY29uc3QgZ2VuZXJhdGVVcmw6IHN0cmluZyA9ICQoJ1tkYXRhLWdlbmVyYXRlLXVybF0nKS5kYXRhKCdnZW5lcmF0ZS11cmwnKSArIGA/d29ya2Zsb3dUeXBlSWQ9JHt3b3JrZmxvd1R5cGVJZH0mYWN0aXZpdHlJZD0ke2FjdGl2aXR5SWR9JnRva2VuTGlmZVNwYW49JHt0b2tlbkxpZmVTcGFufWA7XHJcbiAgICAgICAgY29uc3QgYW50aWZvcmdlcnlIZWFkZXJOYW1lOiBzdHJpbmcgPSAkKCdbZGF0YS1hbnRpZm9yZ2VyeS1oZWFkZXItbmFtZV0nKS5kYXRhKCdhbnRpZm9yZ2VyeS1oZWFkZXItbmFtZScpO1xyXG4gICAgICAgIGNvbnN0IGFudGlmb3JnZXJ5VG9rZW46IHN0cmluZyA9ICQoJ1tkYXRhLWFudGlmb3JnZXJ5LXRva2VuXScpLmRhdGEoJ2FudGlmb3JnZXJ5LXRva2VuJyk7XHJcbiAgICAgICAgY29uc3QgaGVhZGVyczogYW55ID0ge307XHJcblxyXG4gICAgICAgIGhlYWRlcnNbYW50aWZvcmdlcnlIZWFkZXJOYW1lXSA9IGFudGlmb3JnZXJ5VG9rZW47XHJcblxyXG4gICAgICAgICQucG9zdCh7XHJcbiAgICAgICAgICAgIHVybDogZ2VuZXJhdGVVcmwsXHJcbiAgICAgICAgICAgIGhlYWRlcnM6IGhlYWRlcnNcclxuICAgICAgICB9KS5kb25lKHVybCA9PiB7XHJcbiAgICAgICAgICAgICQoJyN3b3JrZmxvdy11cmwtdGV4dCcpLnZhbCh1cmwpO1xyXG4gICAgICAgIH0pO1xyXG4gICAgfTtcclxuXHJcbiAgICAkKCcjZ2VuZXJhdGUtdXJsLWJ1dHRvbicpLm9uKCdjbGljaycsIGUgPT4ge1xyXG4gICAgICAgIGdlbmVyYXRlV29ya2Zsb3dVcmwoKTtcclxuICAgIH0pO1xyXG5cclxuICAgIGlmICgkKCcjd29ya2Zsb3ctdXJsLXRleHQnKS52YWwoKSA9PSAnJykge1xyXG4gICAgICAgIGdlbmVyYXRlV29ya2Zsb3dVcmwoKTtcclxuICAgIH1cclxufSk7Il19
