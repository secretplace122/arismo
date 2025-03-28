// Ожидаем полной загрузки DOM перед выполнением скриптов

document.addEventListener('DOMContentLoaded', function () {
    // ===== БУРГЕР-МЕНЮ =====
    const mobileToggle = document.querySelector('.mobile-menu-toggle');
    const mobileNav = document.querySelector('.mobile-nav');

    // Добавляем обработчики только если элементы существуют
    if (mobileToggle && mobileNav) {
        // Клик по бургер-иконке
        mobileToggle.addEventListener('click', function () {
            this.classList.toggle('active');
            mobileNav.classList.toggle('active');
        });

        // Закрытие меню при клике на пункты
        document.querySelectorAll('.mobile-nav .nav-link').forEach(link => {
            link.addEventListener('click', () => {
                mobileToggle.classList.remove('active');
                mobileNav.classList.remove('active');
            });
        });
    }

    // ===== ЧАСЫ В ЛИЧНОМ КАБИНЕТЕ =====
    const timeElement = document.getElementById('current-time');
    if (timeElement) {
        function updateClock() {
            const now = new Date();
            timeElement.textContent = now.toLocaleTimeString();
        }
        // Обновляем каждую секунду
        setInterval(updateClock, 1000);
        updateClock(); // Показываем время сразу
    }

    // ===== ИНИЦИАЛИЗАЦИЯ ТЕСТОВ =====
    const testForm = document.getElementById('test-form');
    if (testForm) {
        initTest();
    }
});

// ===== ФУНКЦИЯ ДЛЯ ТЕСТОВ =====
function initTest() {
    const testForm = document.getElementById('test-form');
    if (!testForm) return;

    // Таймер теста
    if (window.testData?.timeLeft !== undefined) {
        const timerElement = document.getElementById('time');
        if (timerElement) {
            let timeLeft = Math.max(0, window.testData.timeLeft);

            const updateTimer = () => {
                const minutes = Math.floor(timeLeft / 60);
                const seconds = timeLeft % 60;
                timerElement.textContent = `${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;

                if (timeLeft <= 0) {
                    clearInterval(timerInterval);
                    testForm.submit();
                }
                timeLeft--;
            };

            updateTimer();
            const timerInterval = setInterval(updateTimer, 1000);
        }
    }

    // Выбор ответов
    document.querySelectorAll('.answer').forEach(answer => {
        answer.addEventListener('click', function () {
            const question = this.closest('.question');
            if (!question) return;

            const questionId = question.dataset.questionId;
            const answerId = this.dataset.answerId;

            // Снимаем выделение с других ответов
            question.querySelectorAll('.answer').forEach(a => {
                a.classList.remove('selected', 'bg-primary', 'text-white');
            });

            // Выделяем текущий ответ
            this.classList.add('selected', 'bg-primary', 'text-white');

            // Сохраняем выбор
            const answerInput = document.getElementById(`answer-${questionId}`);
            if (answerInput) {
                answerInput.value = answerId;
            }
        });
    });

    // Навигация между вопросами
    document.querySelectorAll('.btn-next').forEach(btn => {
        btn.addEventListener('click', function () {
            const currentQuestion = this.closest('.question');
            if (!currentQuestion) return;

            const nextQuestion = currentQuestion.nextElementSibling;
            if (nextQuestion?.classList.contains('question')) {
                currentQuestion.classList.remove('active');
                nextQuestion.classList.add('active');
                nextQuestion.scrollIntoView({ behavior: 'smooth' });
            }
        });
    });
}
// ===== НАВИГАЦИЯ НАЗАД =====

function goBack() {
    if (window.history.length > 1) {
        window.history.back();
    } else {
        window.location.href = '/'; // Перенаправляем на главную если нет истории
    }
}
// Показываем лоадер при переходе по ссылкам
document.addEventListener('DOMContentLoaded', function () {
    const links = document.querySelectorAll('a[href^="/"], a[href^="http"]:not([target="_blank"])');
    const loader = document.querySelector('.page-loader');

    links.forEach(link => {
        link.addEventListener('click', function (e) {
            // Исключаем якорные ссылки и ссылки с data-no-loader
            if (!this.getAttribute('href').startsWith('#') &&
                !this.hasAttribute('data-no-loader')) {
                e.preventDefault();
                const href = this.getAttribute('href');

                // Показываем лоадер
                loader.classList.add('active');

                // Задержка для демонстрации лоадера (можно убрать)
                setTimeout(() => {
                    window.location.href = href;
                }, 150);
            }
        });
    });

    // Скрываем лоадер при полной загрузке страницы
    window.addEventListener('load', function () {
        loader.classList.remove('active');
    });
});
