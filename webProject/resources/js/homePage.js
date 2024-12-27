import { loginPage as loginPage, registerPage as registerPage } from '/showSignIn.js';

var vh;
var vw;

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
const leftBtn = document.querySelector("#left-btn");
const rightBtn = document.querySelector("#right-btn");
const search = document.querySelector(".search");
const login = document.getElementById("login");
const register = document.getElementById("register");

const carouselSize = +getComputedStyle(document.documentElement).getPropertyValue('--carousel-size').slice(0, -2); //vh
const gap = +getComputedStyle(document.documentElement).getPropertyValue('--gap').slice(0, -2); //vh
const frameMargin = +getComputedStyle(document.documentElement).getPropertyValue('--frame-margin').slice(0, -2); //vh

let totalOffset = 0; //vh
let minOffset = 0; //vh
let maxOffset = 0; //vh

function pxToVh(px=0) {
    return px / vh * 100;
}

function getFirstElementPose(elements) {
    return elements[0].getBoundingClientRect();
}

function ClassListContains(classList, name) {
    let res = false;
    classList.forEach((el) => {
        if (el === name) {
            res = true;
            return;
        }
    });
    return res;
}

async function asyncForEach(array, callback) {
    for (let i = 0; i < array.length; i++) {
        await callback(array[i], i, array);
    }
}

function setCursor() {
    body.style.cursor = 'pointer';
}

function Hide(element) {
    element.style.opacity = '0';
}

function getDimensionsFromJpeg(arrayBuffer) { // honestly, ChatGPT
    const dataView = new DataView(arrayBuffer);
    let width = 0;
    let height = 0;

    let offset = 2;
    while (offset < dataView.byteLength) {
        const marker = dataView.getUint16(offset);
        offset += 2;

        if (marker >= 0xFFC0 && marker <= 0xFFC3) {
            height = dataView.getUint16(offset + 3);
            width = dataView.getUint16(offset + 5);
            break;
        } else {
            offset += dataView.getUint16(offset);
        }
    }

    return {widthJpeg: width, heightJpeg: height}
}

function Show(element) {
    element.style.opacity = '1';
}

function SlowAppear(element) {
    element.classList.add('appear');
    element.addEventListener("animationend", () => {
        Show(element);
    }, {once: true});
}

function OffDrag(element) {
    element.setAttribute("draggable", "false")
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

    text.appendChild(titleObj);
    text.appendChild(artistObj);

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

function Move(element, offset = 0, min=-Infinity, max=0) {
    document.documentElement.style.setProperty("--total-translate-value", `calc(${Math.min(max, Math.max(totalOffset + offset, min))}vh)`)
    return element;
}

async function searchFrames(query) {
    let srch = await fetch(`/search?${query}`);
    let json = await srch.json();
    return json.data;
}

async function initMuseum(searchQuery) {
    setTotalTranslateValue(0);
    Move(carousel, 0);
    minOffset = 0;
    totalOffset = 0;

    let elements = [];
    let elementSizes = [];
    let elementsPositions = [];
    let currentFrame = undefined;

    function align(width, height) { //vh, without padding
        document.documentElement.style.setProperty('--align-x', `-${0.5 * (carouselSize + 2 * frameMargin) * (width + 2 * frameMargin) / (height + 2 * frameMargin)}vh`);
    }

    const frames = await searchFrames(searchQuery);
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
            const frameObj = createFrame(createImage(imageObjectURL, width / height), frame.title, frame.artistDisplay + "|" + frame.dateDisplay);
            //const frameObj = createImage(imageObjectURL, width / height);
            
            let id = elements.length;
            frameObj.id = `${id}`;

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
                        if(ev.target === carousel){
                            setTotalTranslateValue(currentPos);
                            totalOffset = currentPos;
                            carousel.classList.remove('transition-to-id');
                            carousel.removeEventListener("animationend", transitionEnd);
                        }
                    }

                    carousel.addEventListener("animationend", transitionEnd);
                };

                if (ClassListContains(info.classList, "frame-info-show")) {
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

    function rejectFun(frame) {
        console.log(`${frame.title}, Error fetching image: ${frame.imageId}`)
    }

    let initFrame = async (resolveFun, rejectFun, frame) => {
        const getImageAndAddIt = async (onResolve, onReject) => {
            try {
                const response = await fetch(`/iiif/${frame.imageId}`);
                if (response.ok) {
                    const arrayBuffer = await response.arrayBuffer();
                    const blob = new Blob([arrayBuffer], { type: 'image/jpeg' });
                    onResolve(blob, getDimensionsFromJpeg(arrayBuffer));
                } else {
                    onReject(frame);
                    return;
                }
            } catch {
                onReject(frame);
                return;
            }
        };

        getImageAndAddIt(resolveFun, rejectFun);
    };

    carousel.innerHTML = "";

    frames.forEach(async (fr) => {
        initFrame(resolveFun(fr), rejectFun, fr);
    })

}

initMuseum("Russia");

search.addEventListener('keydown', (ev) => {
    if (ev.key === 'Enter') {
        ev.preventDefault();
        let searchQuery = ev.target.value;
        initMuseum(searchQuery);
    }
});


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
    let lPage = new loginPage(document);
    lPage.show();
});

register.addEventListener('click', () => {
    let rPage = new registerPage(document);
    rPage.show();
});

all.forEach((el) => {
    OffDrag(el);
    el.addEventListener('dragstart', (ev) => {
        ev.preventDefault();
    });
});