class Errors {
    constructor(errorFieldCallback, errors) {
        this.errors = errors;
        this.currentErrors = [];
        this.errorFieldCallback = errorFieldCallback;
        this.hasErrors = false;
    }

    add(error) {
        this.delete(error);
        this.currentErrors.push(error);
        this.update();
    }

    update() {
        if (this.currentErrors.length == 0) {
            this.hasErrors = false;
            this.errorFieldCallback("");
            return;
        }
        this.hasErrors = true;
        this.errorFieldCallback(this.errors[this.currentErrors.at(-1)]);
    }

    delete(error) {
        this.currentErrors = this.currentErrors.filter((item) => item != error) ?? [];
        this.update();
    }
}

export { Errors };