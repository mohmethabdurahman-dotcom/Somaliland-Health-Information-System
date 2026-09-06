// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
ShowModal = (url, title) => {

    $.ajax({
        type: "GET",
        url: url,
        success: function (res) {
            if (res != false) {
                $("#form-modal .modal-body").html(res);
                $('#form-modal .modal-title').html(title);
                $("#form-modal").modal('show');
            }
            else {
                toastr.warning('This Patient have Reservations')
            }

        },
        failure: function (response) {
            alert(response.responseText)
        }, error: function (response) {
            alert(response.responseText)
        }

    })
}


ShowModal1 = (url, title) => {

    $.ajax({
        type: "GET",
        url: url,
        success: function (res) {
            $("#form-modal1 .modal-body").html(res);
            $('#form-modal1 .modal-title').html(title);
            $("#form-modal1").modal('show');
        },
        failure: function (response) {
            alert(response.responseText)
        }, error: function (response) {
            alert(response.responseText)
        }

    })
}

ShowModal2 = (url, title) => {

    $.ajax({
        type: "GET",
        url: url,
        success: function (res) {
            $("#form-modal2 .modal-body").html(res);
            $('#form-modal2 .modal-title').html(title);
            $("#form-modal2").modal('show');
        },
        failure: function (response) {
            alert(response.responseText)
        }, error: function (response) {
            alert(response.responseText)
        }
    })
}

ShowModal3 = (url, title) => {

    $.ajax({
        type: "GET",
        url: url,
        success: function (res) {
            $("#form-modal3 .modal-body").html(res);
            $('#form-modal3 .modal-title').html(title);
            $("#form-modal3").modal('show');
        },
        failure: function (response) {
            alert(response.responseText)
        }, error: function (response) {
            alert(response.responseText)
        }

    })
}

weakly = (url, title) => {

    $.ajax({
        type: "GET",
        url: url,
        success: function (res) {
            $("#form-modal101 .modal-body").html(res);
            $('#form-modal101 .modal-title').html(title);
            $("#form-modal101").modal('show');
        },
        failure: function (response) {
            alert(response.responseText)
        }, error: function (response) {
            alert(response.responseText)
        }

    })
}