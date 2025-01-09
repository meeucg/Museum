async function authFetch(url, info = {}, additionalHeaders = {}) {
    return await fetch(url, {
        headers: {
            "Authorization": localStorage.getItem("token"),
            ...additionalHeaders
        },
        ...info
    });
}

function setJwt(token) {
    localStorage.setItem("token", token);
}

async function getUserInfo() {
    const response = await authFetch("/userinfo", { method: "GET" });
    return response;
}

function getUserLoginObject(login, password, username = "Name placeholder") {
    return {
        "login": login,
        "password": password,
        "username": username
    };
}

export { authFetch, setJwt, getUserInfo, getUserLoginObject }