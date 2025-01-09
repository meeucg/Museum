import { loginPage } from '/showSignIn.js';

const backButton = document.getElementById("back-button");
const changeUser = document.getElementById("register");

backButton.addEventListener("click", (ev) => {
    window.history.back();
});

changeUser.addEventListener('click', () => {
    let lPage = new loginPage(document);
    lPage.show();
});