import { Errors } from "/errorModel.js";
import { postCollection, alertPage } from "/userMethods.js";

const name = document.getElementById("name");
const description = document.getElementById("description");
const errorMessage = document.getElementById("errorMessage");
const submit = document.getElementById("submit");

const errors = {
    0: "Name and surname must be between 2 and 16 charachters",
    3: "Name field must be filled",
    4: "Name field must not equal 'Favorite'"
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

let onSubmit = async () => {
    let desc = "";
    if (description.value == "") {
        desc = "No description"
    } else {
        desc = description.value;
    }
    let result = await postCollection(name.value, desc);

    if (result.ok) {
        triggerEscKeydown();
        return;
    }

    alertPage(document, result.body.error);
}

let handleName = (ev) => {
    let nameVal = name.value;
    if(nameVal.length != 0) {
        if (nameVal == "Favorite") {
            ErrorModel.add(4);
        } else {
            ErrorModel.delete(4);
        }
        ErrorModel.delete(3);
    } else {
        ErrorModel.add(3);
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

ErrorModel.add(3);