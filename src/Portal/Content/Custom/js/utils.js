var utils = (function () {

    /* Private */
    var timeout = null;

    /* Public */
    return {

        // Serves for simple ajax calling
        // Additional arguments: showWaiting
        ajax: function (config) {

            // Show waiting animation
            if (config.showWaiting == true)
                utils.showWaiting();

            // Store base method
            var error = config.error;
            var complete = config.complete;

            // Set arguments
            if (!config.type)
                config.type = "POST";
            if (!config.contentType)
                config.contentType = "application/json; charset=utf-8";
            if (!config.dataType)
                config.dataType = "json";
            config.error = function (jqXHR, textStatus, errorThrow) {
                if (jqXHR.responseJSON) {
                    console.log(jqXHR.responseJSON.Exception);
                    utils.showErrorMsg(jqXHR.responseJSON.Message);
                }
                else {
                    var response = $.parseJSON(jqXHR.responseText);

                    console.log(response.Exception);
                    utils.showErrorMsg(response.Message);
                }

                if (error)
                    error(jqXHR, textStatus, errorThrow);
            }
            config.complete = function (jqXHR, textStatus) {
                if (config.showWaiting == true)
                    utils.hideWaiting();

                if (complete)
                    complete(jqXHR, textStatus);
            }

            // Send ajax request
            $.ajax(config);
        },

        // Shows waiting animation
        showWaiting: function () {
            $(".loader").show();
        },

        // Hides waiting animation
        hideWaiting: function () {
            $(".loader").hide();
        },

        // Called when error occurs
        failForm: function (jqXHR, textStatus, errorThrow) {
            console.log(jqXHR.responseJSON.Exception);
            utils.showErrorMsg(jqXHR.responseJSON.Message);
        },

        // Called on ajax form begin
        beginForm: function () {
            utils.showWaiting();
        },

        // Called when ajax form finishes
        completeForm: function () {
            utils.hideWaiting();
        },

        // Displays success message
        showSuccessMsg: function (msg) {
            utils.showMessage(msg, "success");
        },

        // Displays info message
        showInfoMsg: function (msg) {
            utils.showMessage(msg, "info");
        },

        // Displayes error message
        showErrorMsg: function (msg) {
            utils.showMessage(msg, "danger");
        },

        // Displayes a message
        showMessage: function (msg, type) {

            $.notify({
                // options
                icon: null,
                title: '',
                message: msg,
                target: '_blank'
            }, {
                // settings
                element: 'body',
                position: null,
                type: type,
                allow_dismiss: false,
                newest_on_top: true,
                showProgressbar: false,
                placement: {
                    from: "bottom",
                    align: "right"
                },
                offset: 20,
                spacing: 10,
                z_index: 1031,
                delay: 7000,
                timer: 1000,
                url_target: '_blank',
                mouse_over: null,
                animate: {
                    enter: 'animated fadeInUp',
                    exit: 'animated fadeOutRight'
                },
                icon_type: 'class',
                template: '<div data-notify="container" class="alert alert-{0}" role="alert">' +
                '<button type="button" aria-hidden="true" class="close" data-notify="dismiss">×</button>' +
                '<span data-notify="icon"></span> ' +
                '<span data-notify="title">{1}</span> ' +
                '<span data-notify="message">{2}</span>' +
                '<div class="progress" data-notify="progressbar">' +
                '<div class="progress-bar progress-bar-{0}" role="progressbar" aria-valuenow="0" aria-valuemin="0" aria-valuemax="100" style="width: 0%;"></div>' +
                '</div>' +
                '<a href="{3}" target="{4}" data-notify="url"></a>' +
                '</div>'
            });
        },
    };
})(); 