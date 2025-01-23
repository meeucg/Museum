import { headerFrame } from '/showFrame.js';
import { ClassListContains, getDimensionsFromJpeg, SlowAppear, OffDrag, Hide } from '/utilityFunctions.js';
import { alertPage, checkAuth, getApiLikes, getUserInfo } from '/userMethods.js';

var vh;
var vw;
var isAuthorized = false;
var apiLikes = [];

function updateViewportDimensions() {
    vh = window.innerHeight;
    vw = window.innerWidth;
}

updateViewportDimensions();

window.addEventListener('resize', updateViewportDimensions);

const all = document.querySelectorAll("*");
const body = document.querySelector("body");
const carouselContainer = document.querySelector(".center-container");
const carousel = document.querySelector(".carousel-container");
const search = document.querySelector(".search");
const login = document.getElementById("login");
const register = document.getElementById("register");
const headerRight = document.querySelector(".right-header-container");

const carouselSize = +getComputedStyle(document.documentElement).getPropertyValue('--carousel-size').slice(0, -2); //vh
const gap = +getComputedStyle(document.documentElement).getPropertyValue('--gap').slice(0, -2); //vh
const frameMargin = +getComputedStyle(document.documentElement).getPropertyValue('--frame-margin').slice(0, -2); //vh

let totalOffset = 0; //vh
let minOffset = 0; //vh
let maxOffset = 0; //vh

function pxToVh(px = 0) {
    return px / vh * 100;
}

function createImage(imageUrl, aspectRatio = 1) {
    let image = document.createElement('div');
    image.className = "image";
    image.style.backgroundImage = `url(${imageUrl})`;
    image.style.aspectRatio = aspectRatio;
    return image;
}

function createFrame(imageObj, title, artist) {
    let frame = document.createElement('div');
    frame.className = "image-container";

    let text = document.createElement('div');
    text.style.gap = `calc(var(--frame-margin) / 2)`;
    text.className = "frame-info-hide";
    text.id = "text";

    let titleObj = document.createElement('div');
    titleObj.className = "text-primary-bold";
    titleObj.textContent = `${title}`;

    let artistObj = document.createElement('div');
    artistObj.className = "text-secondary";
    artistObj.textContent = `${artist}`;

    if (isAuthorized == true) {
        let likeButton = document.createElement('div');
        likeButton.className = "like-button";
        likeButton.id = "like";

        let plusButton = document.createElement('div');
        plusButton.className = "plus-button";
        plusButton.id = "plus";

        let buttonContainer = document.createElement('div');
        buttonContainer.className = "button-container";

        buttonContainer.appendChild(likeButton);
        buttonContainer.appendChild(plusButton);

        text.appendChild(titleObj);
        text.appendChild(artistObj);
        text.appendChild(buttonContainer);
    } else {
        text.appendChild(titleObj);
        text.appendChild(artistObj);
    }
    
    frame.appendChild(imageObj);
    frame.appendChild(text);

    return frame;
}

function setTotalTranslateValue(value) { // vh
    document.documentElement.style.setProperty("--total-translate-value", `${value}vh`);
}

function setCurrentFramePos(value) { // vh
    document.documentElement.style.setProperty("--current-frame-pos", `${value}vh`);
}

function Move(element, offset = 0, min = -Infinity, max = 0) {
    document.documentElement.style.setProperty("--total-translate-value", `calc(${Math.min(max, Math.max(totalOffset + offset, min))}vh)`)
    return element;
}

class Museum {

    states = {
        "Cancelled": 0,
        "CancelScheduled": 1,
        "Running": 2,
        "Finished": 3
    }

    constructor(query) {
        this.query = query ?? "Russia";
        this.cycleState = this.states.Cancelled;

        this.elements = [];
        this.elementSizes = [];
        this.elementsPositions = [];

        this.frames = [];
        this.currentFrame = undefined;

        this.controller = new AbortController();
        this.signal = this.controller.signal;
    }

    async searchFrames(query) {
        try{
            let signal = this.signal;
            let srch = await fetch(`/search?${query}`, {signal});
            let json = await srch.json();
            return json.data;
        }
        catch(er){
            console.error(er);
            this.cycleState = this.states.Cancelled;
            return undefined;
        }
    }

    async asyncForEach(array, callback, breakCondition) {
        for (let i = 0; i < array.length; i++) {
            if(breakCondition()){
                this.cycleState = this.states.Cancelled;
                return;
            }
            await callback(array[i], i, array);
        }
        this.cycleState = this.states.Finished;
    }

    setQuery(query) {
        this.query = query;
    }

    updateLikeButtons() {
        let frameObjs = document.querySelectorAll(".carousel-container > .image-container");
        frameObjs.forEach((frameObj) => {
            let text = frameObj.querySelector("#text");
            let pictureId = this.frames[+frameObj.id].id;
            if (text.querySelector(".button-container") == undefined) {
                let likeButton = document.createElement('div');
                if (apiLikes.includes(pictureId)) {
                    likeButton.className = "like-button-active";
                }
                else {
                    likeButton.className = "like-button";
                }
                likeButton.id = "like";

                let plusButton = document.createElement('div');
                plusButton.className = "plus-button";
                plusButton.id = "plus";

                let buttonContainer = document.createElement('div');
                buttonContainer.className = "button-container";

                buttonContainer.appendChild(likeButton);
                buttonContainer.appendChild(plusButton);

                text.appendChild(buttonContainer);
            } else {
                if (apiLikes.includes(pictureId)) {
                    let likeButton = text.querySelector(".button-container > #like");
                    likeButton.className = "like-button-active";
                }
            }
        });
    }

    async initMuseum() {
        this.frames = await this.searchFrames(this.query);
        if(this.frames === undefined){
            return;
        }

        setTotalTranslateValue(0);
        Move(carousel, 0);
        minOffset = 0;
        totalOffset = 0;

        this.elements = [];
        this.elementSizes = [];
        this.elementsPositions = [];
        this.currentFrame = undefined;

        let elements = this.elements;
        let elementSizes = this.elementSizes;
        let elementsPositions = this.elementsPositions;
        let currentFrame = this.currentFrame;
        let frames = this.frames;

        function align(width, height) { //vh, without padding
            document.documentElement.style.setProperty('--align-x', `-${0.5 * (carouselSize + 2 * frameMargin) * (width + 2 * frameMargin) / (height + 2 * frameMargin)}vh`);
        }

        function addFrame(frame) {
            carousel.appendChild(frame);
            elements.push(frame);
            SlowAppear(frame);
        }

        function resolveFun(frame) {
            return (imgBlob, { widthJpeg, heightJpeg }) => {

                let width = carouselSize * widthJpeg / heightJpeg; //vh
                let height = carouselSize; //vh

                const imageObjectURL = URL.createObjectURL(imgBlob);
                const frameObj = createFrame(
                    createImage(imageObjectURL, width / height),
                    frame.title,
                    frame.artistDisplay + " | " + frame.dateDisplay
                );

                let id = elements.length;
                frameObj.id = `${id}`;
                let pictureId = frames[+frameObj.id].id;
                if (apiLikes.includes(pictureId)) {
                    let likeButton = frameObj.querySelector("#text > .button-container > #like");
                    likeButton.className = "like-button-active";
                }

                elementSizes.push({ width: width, height: height });

                let frameWidthById = (id) => {
                    return (carouselSize + 2 * frameMargin) * (elementSizes[id].width + 2 * frameMargin) / (elementSizes[id].height + 2 * frameMargin)
                }

                if (id == 0) {
                    align(width, height);
                    elementsPositions.push(0);
                } else {
                    minOffset -= frameWidthById(id - 1) / 2 + frameWidthById(id) / 2 + gap;
                    elementsPositions.push(minOffset);
                }

                frameObj.addEventListener("click", (ev) => {
                    const info = document.querySelector(`.image-container[id="${id}"] > div[id="text"]`);
                    let currentPos = elementsPositions[+frameObj.id];

                    let transitionToId = () => {
                        setCurrentFramePos(currentPos);
                        carousel.classList.add('transition-to-id');

                        let transitionEnd = (ev) => {
                            if (ev.target === carousel) {
                                setTotalTranslateValue(currentPos);
                                totalOffset = currentPos;
                                carousel.classList.remove('transition-to-id');
                                carousel.removeEventListener("animationend", transitionEnd);
                            }
                        }

                        carousel.addEventListener("animationend", transitionEnd);
                    };

                    if (ClassListContains(info.classList, "frame-info-show")) {
                        if (ev.target.id == "like") {
                            if (ClassListContains(ev.target.classList, "like-button")) {
                                fetch("/likeartic", {
                                    method: "POST",
                                    body: JSON.stringify(
                                        {
                                            id: +frames[+frameObj.id].id
                                        }
                                    )
                                }).then(res => {
                                    if (res.ok) {
                                        ev.target.className = "like-button-active";
                                    }
                                });
                                return;
                            } else {
                                fetch("/dislikeartic", {
                                    method: "POST",
                                    body: JSON.stringify(
                                        {
                                            id: +frames[+frameObj.id].id
                                        }
                                    )
                                }).then(res => {
                                    if (res.ok) {
                                        ev.target.className = "like-button";
                                    }
                                });
                                return;
                            }
                            
                        } else if (ev.target.id == "plus") {
                            return;
                        }

                        if (currentFrame === frameObj &&
                            Math.abs(Math.abs(totalOffset) - Math.abs(currentPos)) > 10) {
                            transitionToId();
                        }
                        else {
                            info.className = "frame-info-hide";
                            info.style.height = `0px`;
                        }
                    }
                    else {
                        if (currentFrame !== undefined) {
                            let cInfo = document.querySelector(`.image-container[id="${currentFrame.id}"] > div[id="text"]`);
                            cInfo.className = "frame-info-hide";
                            cInfo.style.height = `0px`;
                        }
                        currentFrame = frameObj;

                        transitionToId();

                        info.className = "frame-info-show";
                        info.style.height = `${info.scrollHeight}px`;
                    }

                });

                addFrame(frameObj);
            }
        }

        function rejectFun(frame, err = "") {
            console.error(`${frame.title}, Error, imageId: ${frame.imageId}.` + err)
        }

        let initFrame = async (resolveFun, rejectFun, frame) => {
            const signal = this.signal;

            const getImageAndAddIt = async (onResolve, onReject) => {
                try {
                    const response = await fetch(`/iiif?${frame.imageId}`, {signal})
                        .catch(error => {
                            if (error.name === 'AbortError') {
                                onReject(frame, "Init aborted");
                            } else {
                                onReject(frame, "Error fetching image");
                            }
                        });
                    if (response.ok && this.cycleState == this.states.Running) {
                        const arrayBuffer = await response.arrayBuffer();
                        const blob = new Blob([arrayBuffer], { type: 'image/jpeg' });
                        onResolve(blob, getDimensionsFromJpeg(arrayBuffer));
                    } else {
                        onReject(frame, "Fetching error");
                        return;
                    }
                } catch {
                    onReject(frame, "Unknown error");
                    return;
                }
            };

            await getImageAndAddIt(resolveFun, rejectFun);
        };

        carousel.innerHTML = "";

        // frames.forEach(async (fr) => {
        //     await initFrame(resolveFun(fr), rejectFun, fr);
        // }) 
        // вот в этом была проблема

        this.cycleState = this.states.Running;

        await this.asyncForEach(frames, async (fr) => {
            await initFrame(resolveFun(fr), rejectFun, fr);
        }, () => {return this.cycleState == this.states.CancelScheduled});
    }

    async breakInit(callback) {
        if(this.cycleState == this.states.Cancelled || 
            this.cycleState == this.states.Finished){
            callback();
            return true;
        }

        this.cycleState = this.states.CancelScheduled;
        this.controller.abort();

        while(this.cycleState != this.states.Cancelled) {
            console.log("Cancelling");
            await new Promise(resolve => {
                setTimeout(resolve, 100)
            });
        }

        this.controller = new AbortController();
        this.signal = this.controller.signal;
        callback();
        return true;
    }
}


let currentMuseum = new Museum("Russia");
currentMuseum.initMuseum();


let initProfileBtn = async () => {
    const profileBtn = document.createElement('div');
    const text = document.createElement('div');
    const profileIcon = document.createElement('img');

    text.textContent = "Profile";
    profileIcon.src = "/account_circle.svg";
    profileIcon.style.paddingLeft = "0.5em";

    profileBtn.className = "rounded-btn-white";
    profileBtn.appendChild(text);
    profileBtn.appendChild(profileIcon);

    let user = await getUserInfo();
    let userId = user.body.id;

    profileBtn.addEventListener("click", () => {
        window.location.href = `/profile?id=${userId}`;
    });

    headerRight.appendChild(profileBtn);
}

let auth = async () => {
    const user = await checkAuth();

    if (user.ok) {
        login.remove();
        register.remove();
        await initProfileBtn();
        let likes = await getApiLikes();
        if (likes.ok) {
            apiLikes = likes.body;
        }
        isAuthorized = true;
        currentMuseum.updateLikeButtons();
        return;
    }

    alertPage(document, user.body.error);
};

auth();

let searchHandler = (ev) => {
    if (ev.key === 'Enter' && currentMuseum.cycleState == currentMuseum.states.Running || 
        currentMuseum.cycleState == currentMuseum.states.Finished) {
        ev.preventDefault();

        let onBreak = () => {
            currentMuseum.setQuery(ev.target.value);
            currentMuseum.initMuseum();
        };
        
        currentMuseum.breakInit(onBreak);
    }
};

search.addEventListener('keydown', searchHandler);

function handleCarouselMoveMouse(ev) {

    let startX = ev.clientX;
    let offsetX = 0;

    let onMouseMove = (ev) => {
        offsetX = pxToVh(ev.clientX - startX);
        Move(carousel, offsetX, minOffset, maxOffset);
    }

    body.addEventListener('mousemove', onMouseMove);

    carouselContainer.addEventListener('mouseup', (ev) => {
        body.removeEventListener('mousemove', onMouseMove);
        carouselContainer.removeEventListener('mousedown', handleCarouselMoveMouse);

        setTimeout(() => carouselContainer.addEventListener('mousedown', handleCarouselMoveMouse), 50);

        totalOffset = Math.min(maxOffset, Math.max(totalOffset + offsetX, minOffset));
    }, { once: true });
}

function handleCarouselMoveTouch(ev) {

}

function handleCarouselMoveWheel(ev) {
    let offsetX = pxToVh(-ev.deltaX);
    Move(carousel, offsetX, minOffset, maxOffset);
    totalOffset = Math.min(maxOffset, Math.max(totalOffset + offsetX, minOffset));
}

carouselContainer.addEventListener('mousedown', handleCarouselMoveMouse);

carouselContainer.addEventListener('touchstart', handleCarouselMoveTouch);

carouselContainer.addEventListener('wheel', handleCarouselMoveWheel);

login.addEventListener('click', () => {
    let lPage = new headerFrame(document, "/loginPage.html", auth);
    lPage.show();
});

register.addEventListener('click', () => {
    let rPage = new headerFrame(document, "/registerPage.html", auth);
    rPage.show();
});

all.forEach((el) => {
    OffDrag(el);
    el.addEventListener('dragstart', (ev) => {
        ev.preventDefault();
    });
});