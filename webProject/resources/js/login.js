import { setJwt, getUserLoginObject } from '/userMethods.js';

const email = document.getElementById("email");
const password = document.getElementById("password");
const errorMessage = document.getElementById("errorMessage");
const submit = document.getElementById("submit");

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

class Errors {
    constructor(errorFieldCallback, errors) {
        this.errors = errors;
        this.currentErrors = [];
        this.errorFieldCallback = errorFieldCallback;
        this.hasErrors = false;
    }

    add(error) {
        this.delete(error);
        this.currentErrors.push(error);
        this.update();
    }

    update() {
        if (this.currentErrors.length == 0) {
            this.hasErrors = false;
            this.errorFieldCallback("");
            return;
        }
        this.hasErrors = true;
        this.errorFieldCallback(errors[this.currentErrors.at(-1)]);
    }

    delete(error) {
        this.currentErrors = this.currentErrors.filter((item) => item != error) ?? [];
        this.update();
    }
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

    console.log(getUserLoginObject(email.value, password.value));

    let result = await fetch('/login', {
        method: 'POST',
        headers: {},
        body: JSON.stringify(getUserLoginObject(email.value, password.value))
    });

    let responseData = await result.json();

    if (result.status === 200) {
        setJwt(responseData.token);
        triggerEscKeydown();
        return;
    }

    alert(responseData.error);
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
            alert("Check errors!");
        }
    }
});

submit.addEventListener("click", (ev) => {
    ev.preventDefault();
    if (!ErrorModel.hasErrors) {
        onSubmit();
    } else {
        alert("Check errors!");
    }
});

ErrorModel.add(3);