import { headerFrame } from '/showFrame.js';

function checkCookie(name) {
    const cookies = document.cookie.split(';');
    for (let i = 0; i < cookies.length; i++) {
        const cookie = cookies[i].trim();
        if (cookie.startsWith(name + '=')) {
            return true;
        }
    }
    return false;
}

async function checkAuth() {
    const auth = await fetch("/checkauth", {
        method: "GET"
    })
    if (auth.ok) {
        return { ok: true, body: undefined };
    }
    return { ok: false, body: await auth.json() };
}

async function getApiLikes() {
    const likesResponse = await fetch("/userarticlikes");
    const likes = await likesResponse.json();
    return { ok: likesResponse.ok, body: likes }
}

async function getUserInfo() {
    const response = await fetch("/userinfo", { method: "GET" });
    const info = await response.json()
    return { ok: response.ok, body: info };
}

async function likeApi(id) {
    return fetch("/likeartic", {
        method: "POST",
        body: JSON.stringify({
            id: id
        })
    });
}

async function dislikeApi(id) {
    return fetch("/dislikeartic", {
        method: "POST",
        body: JSON.stringify({
            id: id
        })
    });
}

async function getAllUserCollections() {
    const collections = await fetch("", {
        method: "GET",
        body: JSON.stringify({

        })
    });
    return { ok: collections.ok, body: await collections.json() };
}

async function addToCollectionApi(pictureId, collectionId) {
    const res = await fetch(`/addtocollectionartic?collectionid=${collectionId}&pictureid=${pictureId}`, {
        method: "POST"
    });
    if (res.ok) {
        return { ok: true, body: undefined };
    }
    return { ok: false, body: await res.json() };
}

async function postUsername(username) {
    const usernameRes = await fetch("/username", {
        method: "POST",
        body: JSON.stringify({
            username: username
        })
    })
    if (usernameRes.ok) {
        return { ok: true, body: undefined };
    }
    return { ok: false, body: await usernameRes.json() };
}

async function postBanner(base64String, filetype) {
    let res = await fetch("/banner", {
        method: 'POST',
        body: JSON.stringify({
            imageBase64: base64String,
            contentType: filetype
        })
    });
    return { ok: res.ok };
}

async function postUserPicture(base64String, filetype, name, description) {
    let res = await fetch("/userpicture", {
        method: 'POST',
        body: JSON.stringify({
            imageBase64: base64String,
            contentType: filetype,
            name: name,
            description: description
        })
    });
    return { ok: res.ok };
}

async function postCollection(name, description) {
    const add = await fetch("/addcollection", {
        method: "POST",
        body: JSON.stringify({
            name: name,
            description: description
        })
    })
    if (add.ok) {
        return { ok: true, body: undefined };
    }
    return { ok: false, body: await add.json() };
}

async function removeCollection(id) {
    const remove = await fetch(`/removecollection?collectionid=${id}`, {
        method: "POST"
    })
    if (remove.ok) {
        return { ok: true, body: undefined };
    }
    return { ok: false, body: await remove.json() };
}

function alertPage(document, text) {
    let alertPage = new headerFrame(document, "/alertPage.html",
        () => { },
        (doc) => {
            const textField = doc.getElementById("text");
            textField.textContent = text;

            const ok = doc.getElementById("submit");
            ok.addEventListener("click", () => {
                const escEvent = new KeyboardEvent('keydown', {
                    key: 'Escape',
                    keyCode: 27,
                    which: 27,
                    code: 'Escape',
                    bubbles: true,
                    cancelable: true
                });
                ok.dispatchEvent(escEvent);
            })
        }
    );
    alertPage.show();
}

function exitAccount() {
    document.cookie = "token=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/";
}

function getUserLoginObject(login, password, username = "Name placeholder") {
    return {
        "login": login,
        "password": password,
        "username": username
    };
}

export { addToCollectionApi, postUserPicture, removeCollection, postCollection ,getUserInfo, getUserLoginObject, exitAccount, alertPage, likeApi, dislikeApi, getAllUserCollections, getApiLikes, checkAuth, postUsername, postBanner }