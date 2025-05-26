$(document).ready(function () {
    const bookingsUri = '/api/Bookings';
    const carsUri = '/api/Cars';

    // Функція для показу Bootstrap alert
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

    // Завантаження автомобілів для форми створення
    function loadCarsForCreate(selectedCarId = null) {
        console.log('Loading cars for create form from:', carsUri);
        $.ajax({
            url: carsUri,
            type: 'GET',
            headers: {
                'Accept': 'application/json'
            },
            success: function (data) {
                console.log('Received cars:', data);
                const carsArray = data.$values || data;
                if (!Array.isArray(carsArray)) {
                    console.error('Cars data is not an array:', data);
                    showAlert('Некоректний формат даних автомобілів.');
                    return;
                }
                const carSelect = $('#add-car-id');
                if (!carSelect.length) {
                    console.error('Element #add-car-id not found');
                    return;
                }
                carSelect.html('<option value="">Виберіть автомобіль</option>');
                if (carsArray.length === 0) {
                    console.warn('No cars available');
                    carSelect.append('<option value="" disabled>Немає доступних автомобілів</option>');
                    return;
                }
                carsArray.forEach(car => {
                    if (car.statusId === 1) {
                        const selected = selectedCarId && car.id === parseInt(selectedCarId) ? 'selected' : '';
                        carSelect.append(`<option value="${car.id}" ${selected}>${car.brand} ${car.model}</option>`);
                    }
                });
                console.log('Cars loaded into select:', carSelect[0].options.length - 1);
            },
            error: function (xhr) {
                console.error('Unable to load cars:', xhr.responseJSON?.message || xhr.statusText);
                showAlert('Помилка при завантаженні автомобілів: ' + (xhr.responseJSON?.message || 'Невідома помилка.'));
            }
        });
    }

    // Завантаження автомобілів для форми редагування
    function loadCarsForEdit(bookingCarId) {
        console.log('Loading cars for edit form from:', carsUri);
        $.ajax({
            url: carsUri,
            type: 'GET',
            headers: {
                'Accept': 'application/json'
            },
            success: function (data) {
                console.log('Received cars:', data);
                const carsArray = data.$values || data;
                if (!Array.isArray(carsArray)) {
                    console.error('Cars data is not an array:', data);
                    showAlert('Некоректний формат даних автомобілів.');
                    return;
                }
                const carSelect = $('#edit-car-id');
                if (!carSelect.length) {
                    console.error('Element #edit-car-id not found');
                    return;
                }
                carSelect.html('<option value="">Виберіть автомобіль</option>');
                if (carsArray.length === 0) {
                    console.warn('No cars available');
                    carSelect.append('<option value="" disabled>Немає доступних автомобілів</option>');
                    return;
                }
                carsArray.forEach(car => {
                    if (car.statusId === 1 || car.id === bookingCarId) {
                        const selected = car.id === bookingCarId ? 'selected' : '';
                        carSelect.append(`<option value="${car.id}" ${selected}>${car.brand} ${car.model}</option>`);
                    }
                });
                console.log('Cars loaded into select:', carSelect[0].options.length - 1);
            },
            error: function (xhr) {
                console.error('Unable to load cars:', xhr.responseJSON?.message || xhr.statusText);
                showAlert('Помилка при завантаженні автомобілів: ' + (xhr.responseJSON?.message || 'Невідома помилка.'));
            }
        });
    }

    // Завантаження даних бронювання для редагування
    function loadBookingForEdit(id) {
        console.log('Loading booking with ID:', id);
        $.ajax({
            url: `${bookingsUri}/${id}`,
            type: 'GET',
            headers: {
                'Accept': 'application/json'
            },
            success: function (data) {
                console.log('Received booking:', data);
                $('#edit-id').val(data.id);
                $('#edit-username').val(data.userName);
                $('#edit-start-date').val(new Date(data.startDate).toISOString().split('T')[0]);
                $('#edit-end-date').val(new Date(data.endDate).toISOString().split('T')[0]);
                $('#edit-status').val(data.statusId);
                loadCarsForEdit(data.carId);
            },
            error: function (xhr) {
                console.error('Unable to load booking:', xhr.responseJSON?.message || xhr.statusText);
                showAlert('Помилка при завантаженні бронювання: ' + (xhr.responseJSON?.message || 'Невідома помилка.'));
                $('#editBookingForm').html('<div class="text-danger">Помилка при завантаженні даних.</div>');
            }
        });
    }

    // Обробка кнопок видалення
    $(document).on('click', '.delete-btn', function () {
        var bookingId = $(this).data('booking-id');
        var bookingUserName = $(this).data('booking-username');
        console.log('Delete button clicked: bookingId=' + bookingId + ', userName=' + bookingUserName);
        if (!bookingId || !bookingUserName) {
            console.error('Missing booking ID or username');
            showAlert('Помилка: дані бронювання некоректні.');
            return;
        }
        $('#deleteBookingUserName').text(bookingUserName);
        $('#confirmDeleteBtn').data('booking-id', bookingId);
        var modal = $('#deleteBookingModal');
        if (!modal.length) {
            console.error('Modal #deleteBookingModal not found');
            showAlert('Помилка: модальне вікно для видалення не знайдено.');
            return;
        }
        modal.modal('show');
    });

    // Підтвердження видалення
    $(document).on('click', '#confirmDeleteBtn', function () {
        var bookingId = $(this).data('booking-id');
        var $triggerButton = $('.delete-btn[data-booking-id="' + bookingId + '"]');
        console.log('Confirm delete clicked: bookingId=' + bookingId);
        if (!bookingId) {
            console.error('Booking ID is undefined');
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
                console.log('Delete success:', response.message);
                $('#deleteBookingModal').modal('hide');
                showAlert(response.message || 'Бронювання успішно видалено.', 'success');
                setTimeout(() => {
                    location.reload();
                }, 2000);
            },
            error: function (xhr) {
                console.error('Delete error:', xhr.responseJSON?.message || xhr.statusText);
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
        console.log('Calling loadCarsForCreate with carId:', carId);
        loadCarsForCreate(carId);
    }

    // Ініціалізація завантаження даних для редагування
    if ($('#edit-id').length) {
        const bookingId = $('#edit-id').val();
        if (bookingId) {
            console.log('Initializing edit form for booking ID:', bookingId);
            loadBookingForEdit(bookingId);
        }
    }
});