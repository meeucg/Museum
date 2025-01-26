import { getUserLoginObject, alertPage } from '/userMethods.js';
import { Errors } from "/errorModel.js";

const email = document.getElementById("email");
const password1 = document.getElementById("password1");
const password2 = document.getElementById("password2");
const errorMessage = document.getElementById("errorMessage");
const submit = document.getElementById("submit");
const login = document.getElementById("login");

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
    1: "Passwords don't match",
    2: "Password must be between 8 and 20 characters, at least one digit, special symbol, and upper case letter.",
    3: "All the fields must be filled"
}

const ErrorModel = new Errors((error) => {
    errorMessage.innerText = error;
}, errors);

function validateEmail(email) {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
}

function validatePassword(password) {
    const passwordRegex = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$/;
    return passwordRegex.test(password);
}

let onSubmit = async () => {
    let result = await fetch('/register', {
        method: 'POST',
        headers: {},
        body: JSON.stringify(getUserLoginObject(email.value, password1.value))
    });

    let responseData = await result.json();

    if (result.status === 200) {
        triggerEscKeydown();
        return;
    }

    alertPage(document, responseData.error);
}

let handleMatch = (ev) => {
    let val1 = password1.value;
    let val2 = password2.value;
    let val3 = email.value;

    if (val1 != val2) {
        ErrorModel.add(1);
    } else {
        ErrorModel.delete(1);
    }

    if (!validatePassword(val1)) {
        ErrorModel.add(2);
    } else {
        ErrorModel.delete(2);
    }

    if (val1.length != 0 && val2.length != 0 && val3.length != 0) {
        ErrorModel.delete(3);
    } else {
        ErrorModel.add(3);
    }
}

let handleEmail = (ev) => {
    let val1 = email.value;
    let val2 = password1.value;
    let val3 = password2.value;

    if (!validateEmail(val1)) {
        ErrorModel.add(0);
    } else {
        ErrorModel.delete(0);
    }

    if (val1.length != 0 && val2.length != 0 && val3.length != 0) {
        ErrorModel.delete(3);
    } else {
        ErrorModel.add(3);
    }
}

password2.addEventListener("input", handleMatch);
password1.addEventListener("input", handleMatch);
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

login.addEventListener("click", () => {
    location.pathname = "/loginPage.html";
});

ErrorModel.add(3);