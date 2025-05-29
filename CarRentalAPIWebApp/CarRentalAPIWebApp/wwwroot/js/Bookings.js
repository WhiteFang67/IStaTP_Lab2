$(document).ready(function () {
    const bookingsUri = '/api/Bookings';
    const carsUri = '/api/Cars';

    // Функція для показу Bootstrap alert
    function showAlert(message, type = 'danger') {
        var alertHtml = `
            <div class="alert alert-${type}" alert-dismissible fade show" role="alert">
                ${message}
                <button type="button" data-bs-dismiss="alert" aria-label="Close"></button>
            </div>
           `;
        $('#alertContainer').html(alertHtml);
        setTimeout(() => {
            $('#alertContainer .alert').alert('close');
        }, 5000);
    }

    // Завантаження автомобілів для форми створення
    function loadCarsForCreate(selectedCarId = null) {
        $.ajax({
            url: carsUri,
            type: 'GET',
            headers: {
                'Accept': 'application/json'
            },
            success: function (data) {
                const carsArray = data.$values || data;
                if (!Array.isArray(carsArray)) {
                    showAlert('Некоректний формат даних автомобілів.');
                    return;
                }
                const carSelect = $('#add-car-id');
                if (!carSelect.length) {
                    return;
                }
                carSelect.html('<option value="">Виберіть автомобіль</option>');
                if (carsArray.length === 0) {
                    carSelect.append('<option value="" disabled>Немає доступних автомобілів</option>');
                    return;
                }
                carsArray.forEach(car => {
                    if (car.statusId === 1) {
                        const selected = selectedCarId && car.id === parseInt(selectedCarId) ? 'selected' : '';
                        carSelect.append(`< option value = "${car.id}" ${ selected }> ${ car.brand } ${ car.model }</option > `);
                    }
                });
            },
            error: function (xhr) {
                showAlert('Помилка при завантаженні автомобілів: ' + (xhr.responseJSON?.message || 'Невідома помилка.'));
            }
        });
    }

    // Завантаження автомобілів для форми редагування
    function loadCarsForEdit(bookingCarId) {
        $.ajax({
            url: `${ carsUri } /${bookingCarId}`,
        type: 'GET',
            headers: {
            'Accept': 'application/json'
        },
        success: function (car) {
            const carSelect = $('#edit-car-id');
            if (!carSelect.length) {
                return;
            }
            // Відображаємо тільки поточне авто і робимо поле неактивним
            carSelect.html(`<option value="${car.id}" selected>${car.brand} ${car.model}</option>`)
                .prop('disabled', true);
            // Додаємо приховане поле для збереження CarId
            if (!$('#edit-car-id-hidden').length) {
                carSelect.after(`<input type="hidden" id="edit-car-id-hidden" name="CarId" value="${car.id}">`);
            }
        },
        error: function (xhr) {
            showAlert('Помилка при завантаженні автомобіля: ' + (xhr.responseJSON?.message || 'Невідома помилка.'));
        }
    });
    }

// Завантаження даних бронювання для редагування
    function loadBookingForEdit(id) {
        $.ajax({
            url: `${bookingsUri}/${id}`,
            type: 'GET',
            headers: {
                'Accept': 'application/json'
            },
            success: function (data) {
                $('#edit-id').val(data.id);
                $('#edit-username').val(data.userName);
                $('#edit-start-date').val(new Date(data.startDate).toISOString().split('T')[0]);
                $('#edit-end-date').val(new Date(data.endDate).toISOString().split('T')[0]);
                $('#edit-status').val(data.statusId);
                // Завантажуємо тільки поточний автомобіль
                loadCarsForEdit(data.carId);
            },
            error: function (xhr) {
                showAlert('Помилка при завантаженні бронювання: ' + (xhr.responseJSON?.message || 'Невідома помилка.'));
                $('#editBookingForm').html('<div class="text-danger">Помилка при завантаженні даних.</div>');
            }
        });
    }

    // Обробка кнопок видалення
    $(document).on('click', '.delete-btn', function () {
        var bookingId = $(this).data('booking-id');
        var bookingUserName = $(this).data('booking-username');
        if (!bookingId || !bookingUserName) {
            showAlert('Помилка: дані бронювання некоректні.');
            return;
        }
        $('#deleteBookingUserName').text(bookingUserName);
        $('#confirmDeleteBtn').data('booking-id', bookingId);
        var modal = $('#deleteBookingModal');
        if (!modal.length) {
            showAlert('Помилка: модальне вікно для видалення не знайдено.');
            return;
        }
        modal.modal('show');
    });

    // Підтвердження видалення
    $(document).on('click', '#confirmDeleteBtn', function () {
        var bookingId = $(this).data('booking-id');
        var $triggerButton = $('.delete-btn[data-booking-id="' + bookingId + '"]');
        if (!bookingId) {
            showAlert('Помилка: ID бронювання не визначено.');
            return;
        }
        $.ajax({
            url: `${bookingsUri}/${bookingId}`,
            type: 'DELETE',
            headers: {
                'Accept': 'application/json'
            },
            success: function (response) {
                $('#deleteBookingModal').modal('hide');
                showAlert(response.message || 'Бронювання успішно видалено.', 'success');
                setTimeout(() => {
                    location.reload();
                }, 2000);
            },
            error: function (xhr) {
                $('#deleteBookingModal').modal('hide');
                showAlert('Помилка: ' + (xhr.responseJSON?.message || 'Не вдалося видалити бронювання.'));
            },
            complete: function () {
                if ($triggerButton.length) {
                    $triggerButton.focus();
                }
            }
        });
    });

    // Ініціалізація завантаження автомобілів для форми створення
    if ($('#add-car-id').length) {
        const urlParams = new URLSearchParams(window.location.search);
        const carId = urlParams.get('carId');
        loadCarsForCreate(carId);
    }

    // Ініціалізація завантаження даних для редагування
    if ($('#edit-id').length) {
        const bookingId = $('#edit-id').val();
        if (bookingId) {
            loadBookingForEdit(bookingId);
        }
    }
});