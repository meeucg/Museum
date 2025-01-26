import { Errors } from "/errorModel.js";
import { postUsername, alertPage, postBanner } from "/userMethods.js";

const name = document.getElementById("name");
const surname = document.getElementById("surname");
const errorMessage = document.getElementById("errorMessage");
const submit = document.getElementById("submit");
const bannerinput = document.getElementById("change-banner");
const bannerpreview = document.getElementById("banner-preview");

const errors = {
    0: "Name and surname must be between 2 and 16 charachters",
    3: "At least one field must be filled",
    4: "Please wait, until the file is loaded"
}

const ErrorModel = new Errors((error) => {
    errorMessage.innerText = error;
}, errors);

function triggerEscKeydown() {
    const escEvent = new KeyboardEvent('keydown', {
        key: 'Escape',
        keyCode: 27,
        which: 27,
        code: 'Escape',
        bubbles: true,
        cancelable: true
    });
    submit.dispatchEvent(escEvent);
}

function uploadFile(file) {
    if (!(file.type == "image/jpg") && !(file.type == "image/jpg") && !(file.type == "image/png")) {
        alertPage(document, "Only jpg/jpeg or png files are allowed");
        return;
    }

    const reader = new FileReader();

    reader.onload = function (event) {
        const arrayBuffer = event.target.result;
        const base64String = arrayBufferToBase64(arrayBuffer);

        postBanner(base64String, file.type);
    };

    function arrayBufferToBase64(buffer) {
        let binary = '';
        const bytes = new Uint8Array(buffer);
        const len = bytes.byteLength;
        for (let i = 0; i < len; i++) {
            binary += String.fromCharCode(bytes[i]);
        }
        return btoa(binary);
    }

    reader.readAsArrayBuffer(file);
}

let onSubmit = async () => {

    const banner = bannerinput.files[0];
    if (banner) { uploadFile(banner); }

    let result = await postUsername(name.value + " " + surname.value);

    if (result.ok) {
        triggerEscKeydown();
        return;
    }

    alertPage(document, result.body.error);
}

let handleName = (ev) => {
    let val1 = name.value;
    let val2 = surname.value;

    if (val1.length != 0 || val2.length != 0) {
        ErrorModel.delete(3);
    } else {
        ErrorModel.add(3);
    }
}

let handleSurname = (ev) => {
    let val1 = name.value;
    let val2 = surname.value;

    if (val1.length != 0 || val2.length != 0) {
        ErrorModel.delete(3);
    } else {
        ErrorModel.add(3);
    }
}

name.addEventListener("input", handleName);
surname.addEventListener("input", handleSurname);

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

bannerinput.addEventListener("change", (ev) => {
    ErrorModel.add(4);
    const file = ev.target.files[0];
    const reader = new FileReader();

    reader.onload = function (event) {
        let url = event.target.result;
        bannerpreview.style.backgroundImage = `url('${url}')`;
        ErrorModel.delete(4);
    };

    reader.readAsDataURL(file);
});

ErrorModel.add(3);