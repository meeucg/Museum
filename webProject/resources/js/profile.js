import { headerFrame } from '/showFrame.js';
import { exitAccount, checkAuth, removeCollection } from "/userMethods.js";

const backButton = document.getElementById("back-button");
const changeUser = document.getElementById("register");
const edit = document.getElementById("edit");
const exit = document.getElementById("exit");
const profileContainer = document.getElementById("profile-container");
const banner = document.querySelector(".profile-banner");
const body = document.querySelector("body");
const saved = document.getElementById("saved");
const created = document.getElementById("created");
const move = document.getElementById("move");
const addCollection = document.getElementById("add-collection");
const addUserPicture = document.getElementById("add-picture");
const deleteButtons = document.querySelectorAll("#delete");
const collections = document.querySelectorAll(".collection-container");

let style = getComputedStyle(profileContainer);
let height = +style.height.slice(0, -2);
let paddingTop = +style.paddingTop.slice(0, -2);
let paddingBottom = +style.paddingBottom.slice(0, -2);

banner.style.height = `${height + paddingTop + paddingBottom}px`;

backButton.addEventListener("click", (ev) => {
    window.location.href = "/home";
});

created.addEventListener("click", () => {
    move.style.translate = "-50vw 0px";
});

saved.addEventListener("click", () => {
    move.style.translate = "50vw 0px";
});

if (addCollection != undefined) {
    addCollection.addEventListener("click", () => {
        let onExit = (body) => {
            return async () => {
                location.reload(false);
                body.style.overflowY = "auto";
            }
        }

        let onLoad = (body) => {
            return () => {
                body.style.overflowY = "hidden";
            }
        }

        let addPage = new headerFrame(document, "/addCollectionPage.html", onExit(body), onLoad(body));
        addPage.show();
    });
}

if (addUserPicture != undefined) {
    addUserPicture.addEventListener("click", () => {
        let onExit = (body) => {
            return async () => {
                location.reload(false);
                body.style.overflowY = "auto";
            }
        }

        let onLoad = (body) => {
            return () => {
                body.style.overflowY = "hidden";
            }
        }

        let addPage = new headerFrame(document, "/addPicturePage.html", onExit(body), onLoad(body));
        addPage.show();
    });
}

if (changeUser != undefined) {
    changeUser.addEventListener('click', () => {
        let onExit = (body) => {
            return async () => {
                let auth = await checkAuth();
                if (auth.ok) {
                    location.reload(false);
                }
                body.style.overflowY = "auto";
            }
        }

        let onLoad = (body) => {
            return () => {
                body.style.overflowY = "hidden";
            }
        }

        let lPage = new headerFrame(document, "/loginPage.html", onExit(body), onLoad(body));
        lPage.show();
    });
}

if (edit != undefined) {
    edit.addEventListener("click", () => {
        let onExit = (body) => {
            return () => {
                body.style.overflowY = "auto";
                location.reload(false);
            }
        }

        let onLoad = (body) => {
            return () => {
                body.style.overflowY = "hidden";
            }
        }

        let editPage = new headerFrame(document, "/editProfile.html", onExit(body), onLoad(body));
        editPage.show();
    });
}

collections.forEach((col) => {
    if (col.parentElement.id == "created-grid") {
        return;
    }
    col.addEventListener("click", async (ev) => {
        if (ev.target.id == "delete") {
            const res = await removeCollection(col.id);
            if (res.ok) {
                col.remove();
            } else {
                alertPage(document, res.body.error);
            }
            return;
        }
        let onExit = (body) => {
            return () => {
                body.style.overflowY = "auto";
            }
        }

        let onLoad = (body) => {
            return () => {
                body.style.overflowY = "hidden";
            }
        }

        let colPage = new headerFrame(document, `/collectionpage?collectionid=${col.id}`, onExit(body), onLoad(body));
        colPage.show();
    });
});

if (exit != undefined) {
    exit.addEventListener("click", () => {
        exitAccount();
        location.reload(false);
    })
}