$(document).ready(function () {
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

    $(document).on('click', '.delete-btn', function () {
        var reviewId = $(this).data('review-id');
        var reviewUserName = $(this).data('review-username');
        $('#deleteReviewUserName').text(reviewUserName);
        $('#confirmDeleteBtn').data('review-id', reviewId);
        $('#deleteReviewModal').modal('show');
    });

    $(document).on('click', '#confirmDeleteBtn', function () {
        var reviewId = $(this).data('review-id');
        var $triggerButton = $('.delete-btn[data-review-id="' + reviewId + '"]');
        if (!reviewId) {
            showAlert('Помилка: ID відгуку не визначено.');
            return;
        }
        $.ajax({
            url: '/api/Reviews/' + reviewId,
            type: 'DELETE',
            success: function (response) {
                $('#deleteReviewModal').modal('hide');
                showAlert('Відгук успішно видалено.', 'success');
                setTimeout(() => {
                    location.reload();
                }, 2000);
            },
            error: function (xhr) {
                $('#deleteReviewModal').modal('hide');
                showAlert('Помилка: ' + (xhr.responseJSON?.message || 'Не вдалося видалити відгук.'));
            },
            complete: function () {
                if ($triggerButton.length) {
                    $triggerButton.focus();
                }
            }
        });
    });

    $(document).on('click', '.btn-warning', function () {
        var reviewId = $(this).data('review-id');
        var reviewUserName = $(this).data('review-username');
        var reviewComment = $(this).data('review-comment');
        if (!reviewId) {
            showAlert('Помилка: ID відгуку не визначено.');
            return;
        }
        $('#edit-id').val(reviewId);
        $('#edit-username').val(reviewUserName);
        $('#edit-comment').val(reviewComment);
        $('#editReviewModal').modal('show');
    });

    // Обробник для форми редагування
    $('#editReviewForm').on('submit', function (e) {
        e.preventDefault();
        // Перевіряємо клієнтську валідацію
        if (!$(this).valid()) {
            return;
        }
        var reviewId = $('#edit-id').val();
        var reviewData = {
            Id: reviewId,
            UserName: $('#edit-username').val().trim(),
            Comment: $('#edit-comment').val().trim(),
            Date: new Date().toISOString()
        };

        $.ajax({
            url: '/api/Reviews/' + reviewId,
            type: 'PUT',
            contentType: 'application/json',
            data: JSON.stringify(reviewData),
            success: function () {
                $('#editReviewModal').modal('hide');
                showAlert('Відгук успішно відредаговано!', 'success');
                setTimeout(() => {
                    location.reload();
                }, 2000);
            },
            error: function (xhr) {
                var response = xhr.responseJSON;
                $('#edit-username-error').text('');
                $('#edit-comment-error').text('');
                if (response) {
                    // Обробка помилок валідації з ModelState
                    if (response['UserName']) {
                        $('#edit-username-error').text(response['UserName'][0]);
                    }
                    if (response['Comment']) {
                        $('#edit-comment-error').text(response['Comment'][0]);
                    }
                    if (!response['UserName'] && !response['Comment']) {
                        showAlert('Помилка: ' + (response.message || 'Не вдалося змінити відгук.'));
                    }
                } else {
                    showAlert('Помилка: Не вдалося змінити відгук.');
                }
            }
        });
    });

    // Обробник для форми створення
    $('#createReviewForm').on('submit', function (e) {
        e.preventDefault();
        // Перевіряємо клієнтську валідацію
        if (!$(this).valid()) {
            return;
        }
        var reviewData = {
            Id: 0,
            UserName: $('#Review_UserName').val().trim(),
            Comment: $('#Review_Comment').val().trim(),
            Date: new Date().toISOString()
        };

        $.ajax({
            url: '/api/Reviews',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(reviewData),
            success: function () {
                showAlert('Відгук успішно додано!', 'success');
                setTimeout(() => {
                    location.reload();
                }, 2000);
            },
            error: function (xhr) {
                var response = xhr.responseJSON;
                $('#create-username-error').text('');
                $('#create-comment-error').text('');
                if (response) {
                    // Обробка помилок валідації з ModelState
                    if (response['UserName']) {
                        $('#create-username-error').text(response['UserName'][0]);
                    }
                    if (response['Comment']) {
                        $('#create-comment-error').text(response['Comment'][0]);
                    }
                    if (!response['UserName'] && !response['Comment']) {
                        showAlert('Помилка: ' + (response.message || 'Не вдалося додати відгук.'));
                    }
                } else {
                    showAlert('Помилка: Не вдалося додати відгук.');
                }
            }
        });
    });
});