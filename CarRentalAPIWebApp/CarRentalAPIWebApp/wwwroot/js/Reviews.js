$(document).ready(function () {
    const reviewsUri = '/api/Reviews';

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

    // Отримання всіх відгуків
    function getReviews() {
        console.log('Fetching reviews from:', reviewsUri);
        $.ajax({
            url: reviewsUri,
            type: 'GET',
            headers: {
                'Accept': 'application/json'
            },
            success: function (data) {
                console.log('Received reviews:', data);
                const reviewsArray = data.$values || data;
                if (!Array.isArray(reviewsArray)) {
                    console.error('Reviews data is not an array:', data);
                    showAlert('Некоректний формат даних відгуків.');
                    return;
                }
                displayReviews(reviewsArray);
            },
            error: function (xhr) {
                console.error('Unable to load reviews:', xhr.responseJSON?.message || xhr.statusText);
                showAlert('Помилка при завантаженні відгуків: ' + (xhr.responseJSON?.message || 'Невідома помилка.'));
                $('#reviews').html('<p class="text-danger">Помилка при завантаженні відгуків.</p>');
            }
        });
    }

    // Відображення відгуків
    function displayReviews(reviews) {
        const reviewsContainer = $('#reviews');
        if (!reviewsContainer.length) {
            console.error('Element #reviews not found');
            showAlert('Помилка: контейнер для відгуків не знайдено.');
            return;
        }
        reviewsContainer.empty();
        if (!reviews || reviews.length === 0) {
            reviewsContainer.html('<p>Наразі немає відгуків.</p>');
            return;
        }
        reviews.forEach(review => {
            const reviewHtml = `
                <div class="card mb-3">
                    <div class="card-body">
                        <h5 class="card-title">${review.UserName}</h5>
                        <p class="card-text">${review.Comment}</p>
                        <p class="card-text"><small class="text-muted">${new Date(review.Date).toLocaleDateString()}</small></p>
                        <button class="btn btn-warning btn-sm edit-btn" data-review-id="${review.Id}">Редагувати</button>
                        <button class="btn btn-danger btn-sm delete-btn" data-bs-toggle="modal" data-bs-target="#deleteReviewModal" data-review-id="${review.Id}" data-review-username="${review.UserName}">Видалити</button>
                    </div>
                </div>
            `;
            reviewsContainer.append(reviewHtml);
        });
    }

    // Додавання відгуку
    $('#addReviewForm').on('submit', function (e) {
        e.preventDefault();
        const userName = $('#add-username').val().trim();
        const comment = $('#add-comment').val().trim();
        console.log('Adding review:', { userName, comment });

        if (!userName || !comment) {
            showAlert('Будь ласка, заповніть усі обов’язкові поля.');
            $('#add-username').toggleClass('is-invalid', !userName);
            $('#add-comment').toggleClass('is-invalid', !comment);
            return;
        }

        $.ajax({
            url: reviewsUri,
            type: 'POST',
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json'
            },
            data: JSON.stringify({
                UserName: userName,
                Comment: comment,
                Date: new Date().toISOString()
            }),
            success: function (response) {
                console.log('Review added:', response);
                showAlert('Відгук успішно додано!', 'success');
                $('#add-username').val('');
                $('#add-comment').val('');
                $('#add-username').removeClass('is-invalid');
                $('#add-comment').removeClass('is-invalid');
                getReviews();
            },
            error: function (xhr) {
                console.error('Unable to add review:', xhr.responseJSON?.message || xhr.statusText);
                showAlert('Помилка при додаванні відгуку: ' + (xhr.responseJSON?.message || 'Невідома помилка.'));
            }
        });
    });

    // Завантаження даних відгуку для редагування
    function loadReviewForEdit(id) {
        console.log('Loading review with ID:', id);
        $.ajax({
            url: `${reviewsUri}/${id}`,
            type: 'GET',
            headers: {
                'Accept': 'application/json'
            },
            success: function (data) {
                console.log('Received review:', data);
                $('#edit-id').val(data.Id);
                $('#edit-username').val(data.UserName);
                $('#edit-comment').val(data.Comment);
                $('#editReviewModal').modal('show');
            },
            error: function (xhr) {
                console.error('Unable to load review:', xhr.responseJSON?.message || xhr.statusText);
                showAlert('Помилка при завантаженні відгуку: ' + (xhr.responseJSON?.message || 'Невідома помилка.'));
            }
        });
    }

    // Обробка кнопок редагування
    $(document).on('click', '.edit-btn', function () {
        var reviewId = $(this).data('review-id');
        console.log('Edit button clicked: reviewId=' + reviewId);
        if (!reviewId) {
            console.error('Review ID is undefined');
            showAlert('Помилка: ID відгуку не визначено.');
            return;
        }
        loadReviewForEdit(reviewId);
    });

    // Оновлення відгуку
    $('#editReviewForm').on('submit', function (e) {
        e.preventDefault();
        const reviewId = $('#edit-id').val();
        const userName = $('#edit-username').val().trim();
        const comment = $('#edit-comment').val().trim();
        console.log('Updating review:', { reviewId, userName, comment });

        if (!userName || !comment) {
            showAlert('Будь ласка, заповніть усі обов’язкові поля.');
            $('#edit-username').toggleClass('is-invalid', !userName);
            $('#edit-comment').toggleClass('is-invalid', !comment);
            return;
        }

        $.ajax({
            url: `${reviewsUri}/${reviewId}`,
            type: 'PUT',
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json'
            },
            data: JSON.stringify({
                Id: parseInt(reviewId),
                UserName: userName,
                Comment: comment,
                Date: new Date().toISOString()
            }),
            success: function (response) {
                console.log('Review updated:', response);
                $('#editReviewModal').modal('hide');
                showAlert('Відгук успішно оновлено!', 'success');
                $('#edit-username').val('');
                $('#edit-comment').val('');
                $('#edit-username').removeClass('is-invalid');
                $('#edit-comment').removeClass('is-invalid');
                getReviews();
            },
            error: function (xhr) {
                console.error('Unable to update review:', xhr.responseJSON?.message || xhr.statusText);
                showAlert('Помилка при оновленні відгуку: ' + (xhr.responseJSON?.message || 'Невідома помилка.'));
            }
        });
    });

    // Обробка кнопок видалення
    $(document).on('click', '.delete-btn', function () {
        var reviewId = $(this).data('review-id');
        var reviewUserName = $(this).data('review-username');
        console.log('Delete button clicked: reviewId=' + reviewId + ', userName=' + reviewUserName);
        if (!reviewId || !reviewUserName) {
            console.error('Missing review ID or username');
            showAlert('Помилка: дані відгуку некоректні.');
            return;
        }
        $('#deleteReviewUserName').text(reviewUserName);
        $('#confirmDeleteBtn').data('review-id', reviewId);
        var modal = $('#deleteReviewModal');
        if (!modal.length) {
            console.error('Modal #deleteReviewModal not found');
            showAlert('Помилка: модальне вікно для видалення не знайдено.');
            return;
        }
        modal.modal('show');
    });

    // Підтвердження видалення
    $(document).on('click', '#confirmDeleteBtn', function () {
        var reviewId = $(this).data('review-id');
        var $triggerButton = $('.delete-btn[data-review-id="' + reviewId + '"]');
        console.log('Confirm delete clicked: reviewId=' + reviewId);
        if (!reviewId) {
            console.error('Review ID is undefined');
            showAlert('Помилка: ID відгуку не визначено.');
            return;
        }
        $.ajax({
            url: `${reviewsUri}/${reviewId}`,
            type: 'DELETE',
            headers: {
                'Accept': 'application/json'
            },
            success: function (response) {
                console.log('Delete success:', response.message);
                $('#deleteReviewModal').modal('hide');
                $('.modal-backdrop').remove(); // Примусово видаляємо оверлей
                showAlert(response.message || 'Відгук успішно видалено.', 'success');
                getReviews();
            },
            error: function (xhr) {
                console.error('Delete error:', xhr.responseJSON?.message || xhr.statusText);
                $('#deleteReviewModal').modal('hide');
                $('.modal-backdrop').remove(); // Примусово видаляємо оверлей
                showAlert('Помилка: ' + (xhr.responseJSON?.message || 'Не вдалося видалити відгук.'));
            },
            complete: function () {
                if ($triggerButton.length) {
                    $triggerButton.focus();
                }
                // Додаткове очищення оверлею
                $('body').removeClass('modal-open');
                $('.modal-backdrop').remove();
            }
        });
    });

    // Ініціалізація
    getReviews();
});