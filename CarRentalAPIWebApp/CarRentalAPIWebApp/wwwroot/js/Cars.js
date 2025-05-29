$(document).ready(function () {
    // Відображає повідомлення, яке автоматично зникає через 5 секунд.
    function showAlert(message, type = 'danger') {
        var alertHtml = `
            <div class="alert alert-${type} alert-dismissible fade show" role="alert">
                ${message}
                <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
            </div>
        `;
        $('#alertContainer').html(alertHtml);
        setTimeout(() => {
            $('#alertContainer .alert').alert('close');
        }, 5000);
    }

    // Обробляє кнопку "Видалити" для автомобіля.
    $(document).on('click', '.delete-btn', function () {
        var carId = $(this).data('car-id');
        var carBrand = $(this).data('car-brand');
        $('#deleteCarBrand').text(carBrand);
        $('#confirmDeleteBtn').data('car-id', carId);
        $('#deleteModal').modal('show');
    });

    // Обробляє підтвердженння видалення
    $(document).on('click', '#confirmDeleteBtn', function () {
        var carId = $(this).data('car-id');
        var $triggerButton = $('.delete-btn[data-car-id="' + carId + '"]');
        if (!carId) {
            showAlert('Помилка: ID автомобіля не визначено.');
            return;
        }
        $.ajax({
            url: '/api/Cars/' + carId,
            type: 'DELETE',
            success: function (response) {
                $('#deleteModal').modal('hide', { backdrop: false });
                showAlert('Автомобіль успішно видалено!', 'success');
                setTimeout(() => {
                    location.reload();
                }, 2000);
            },
            error: function (xhr) {
                $('#deleteModal').modal('hide', { backdrop: false });
                showAlert('Помилка: ' + (xhr.responseJSON?.message || 'Не вдалося видалити автомобіль.'));
            },
            complete: function () {
                if ($triggerButton.length) {
                    $triggerButton.focus();
                }
            }
        });
    });
});