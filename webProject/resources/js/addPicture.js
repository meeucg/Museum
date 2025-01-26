import { Errors } from "/errorModel.js";
import { postUserPicture, alertPage } from "/userMethods.js";

const name = document.getElementById("name");
const description = document.getElementById("description");
const errorMessage = document.getElementById("errorMessage");
const addImage = document.getElementById("add-image");
const image = document.getElementById("image");
const submit = document.getElementById("submit");

const errors = {
    0: "Image file must be selected",
    1: "Name field must be filled",
    2: "Wait until the image is loaded"
}

const ErrorModel = new Errors((error) => {
    errorMessage.innerText = error;
}, errors);

function uploadFileAndPostUserPicture(file, name, description) {
    if (!(file.type == "image/jpg") && !(file.type == "image/jpg") && !(file.type == "image/png")) {
        alertPage(document, "Only jpg/jpeg or png files are allowed");
        return;
    }

    const reader = new FileReader();

    reader.onload = async function (event) {
        const arrayBuffer = event.target.result;
        const base64String = arrayBufferToBase64(arrayBuffer);

        const result = await postUserPicture(base64String, file.type, name, description);
        if (result.ok) {
            triggerEscKeydown();
            return;
        }

        alertPage(document, result.body.error);
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

let onSubmit = async () => {
    let desc = "";
    if (description.value == "") {
        desc = "No description"
    } else {
        desc = description.value;
    }
    
    const file = addImage.files[0];
    uploadFileAndPostUserPicture(file, name.value, desc);
}

let handleName = (ev) => {
    let nameVal = name.value;
    if (nameVal.length != 0) {
        ErrorModel.delete(1);
    } else {
        ErrorModel.add(1);
    }
}

name.addEventListener("input", handleName);

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

addImage.addEventListener("change", (ev) => {
    ErrorModel.add(2);
    ErrorModel.delete(0);
    const file = ev.target.files[0];
    const reader = new FileReader();

    reader.onload = function (event) {
        let url = event.target.result;
        image.style.backgroundImage = `url('${url}')`;
        ErrorModel.delete(2);
    };

    reader.readAsDataURL(file);
});

ErrorModel.add(1);
ErrorModel.add(0);