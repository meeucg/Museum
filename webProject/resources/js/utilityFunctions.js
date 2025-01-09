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
        element.style.opacity = '1';
    }, {once: true});
}

function OffDrag(element) {
    element.setAttribute("draggable", "false")
}

export {
    getFirstElementPose, 
    ClassListContains, 
    asyncForEach, 
    setCursor, 
    Hide, 
    getDimensionsFromJpeg, 
    Show, 
    SlowAppear, 
    OffDrag
};