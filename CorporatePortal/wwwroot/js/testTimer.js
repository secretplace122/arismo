document.addEventListener('DOMContentLoaded', function () {
    if (!window.testData || !window.testData.timeLeft) {
        console.error('Timer data not initialized');
        return;
    }

    const timerElement = document.getElementById('time');
    if (!timerElement) {
        console.error('Timer element not found');
        return;
    }

    let timeLeft = Math.max(0, window.testData.timeLeft);

    function updateTimer() {
        const minutes = Math.floor(timeLeft / 60);
        const seconds = timeLeft % 60;
        timerElement.textContent = `${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;

        if (timeLeft <= 0) {
            document.getElementById('test-form').submit();
            return;
        }

        timeLeft--;
        setTimeout(updateTimer, 1000);
    }

    updateTimer();
});