import { getUserLoginObject, alertPage } from '/userMethods.js';
import { Errors } from "/errorModel.js";

const email = document.getElementById("email");
const password = document.getElementById("password");
const errorMessage = document.getElementById("errorMessage");
const submit = document.getElementById("submit");
const register = document.getElementById("register");

function triggerEscKeydown() {
    const escEvent = new KeyboardEvent('keydown', {
        key: 'Escape',
        keyCode: 27,
        which: 27,
        code: 'Escape',
        bubbles: true,
        cancelable: true
    });
    email.dispatchEvent(escEvent);
}

const errors = {
    0: "Username must be a valid email",
    3: "All the fields must be filled"
}

const ErrorModel = new Errors((error) => {
    errorMessage.innerText = error;
}, errors);

function validateEmail(email) {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
}

let onSubmit = async () => {

    let result = await fetch('/login', {
        method: 'POST',
        headers: {},
        body: JSON.stringify(getUserLoginObject(email.value, password.value))
    });

    let responseData = await result.json();

    if (result.status === 200) {
        triggerEscKeydown();
        return;
    }

    alertPage(document, responseData.error);
}

let handlePassword = (ev) => {
    let val1 = password.value;
    let val2 = email.value;

    if (val1.length != 0 && val2.length != 0) {
        ErrorModel.delete(3);
    } else {
        ErrorModel.add(3);
    }
}

let handleEmail = (ev) => {
    let val1 = email.value;
    let val2 = password.value;

    if (!validateEmail(val1)) {
        ErrorModel.add(0);
    } else {
        ErrorModel.delete(0);
    }

    if (val1.length != 0 && val2.length != 0) {
        ErrorModel.delete(3);
    } else {
        ErrorModel.add(3);
    }
}

password.addEventListener("input", handlePassword);
email.addEventListener("input", handleEmail);

document.addEventListener("keydown", (ev) => {
    if (ev.key == "Enter") {
        ev.preventDefault();
        if (!ErrorModel.hasErrors) {
            onSubmit();
        } else {
            alertPage(document, "Check errors!");
        }
    }
});

submit.addEventListener("click", (ev) => {
    ev.preventDefault();
    if (!ErrorModel.hasErrors) {
        onSubmit();
    } else {
        alertPage(document, "Check errors!");
    }
});

register.addEventListener("click", () => {
    location.pathname = "/registerPage.html";
});

ErrorModel.add(3);