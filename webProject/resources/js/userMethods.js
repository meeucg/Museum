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
    const like = await fetch("/likeartic", {
        method: "POST",
        body: JSON.stringify({
            id: id
        })
    });
    return { ok: like.ok, body: await like.json() };
}

async function dislikeApi(id) {
    const dislike = await fetch("/dislikeartic", {
        method: "POST",
        body: JSON.stringify({
            id: id
        })
    });
    return { ok: dislike.ok, body: await dislike.json() };
}

async function getAllUserCollections() {
    const collections = await fetch("", {
        method: "GET",
        body: JSON.stringify({

        })
    });
    return { ok: collections.ok, body: await collections.json() };
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

export { getUserInfo, getUserLoginObject, exitAccount, alertPage, likeApi, dislikeApi, getAllUserCollections, getApiLikes, checkAuth }