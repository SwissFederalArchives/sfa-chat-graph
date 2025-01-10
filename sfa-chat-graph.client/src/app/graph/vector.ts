export class Vector {

    public x: number = 0;
    public y: number = 0;

    constructor(x: number, y: number) {
        this.x = x;
        this.y = y;
    }

    copy(): Vector {
        return new Vector(this.x, this.y);
    }


    abs(): Vector {
        return new Vector(Math.abs(this.x), Math.abs(this.y));
    }

    add(other: Vector): Vector {
        return new Vector(this.x + other.x, this.y + other.y);
    }

    sub(other: Vector): Vector {
        return new Vector(this.x - other.x, this.y - other.y);
    }

    mul(factor: number): Vector {
        return new Vector(this.x * factor, this.y * factor);
    }

    div(factor: number): Vector {
        return new Vector(this.x / factor, this.y / factor);
    }

    length(): number {
        return Math.sqrt(this.x * this.x + this.y * this.y);
    }

    normalize(): Vector {
        return this.div(this.length());
    }

    mulSet(factor: number): Vector {
        this.x *= factor;
        this.y *= factor;
        return this;
    }

    addSet(vec: Vector): Vector {
        this.x += vec.x;
        this.y += vec.y;
        return this;
    }

    addXYSet(dx: number, dy: number): Vector {
        this.x += dx;
        this.y += dy;
        return this;
    }

    set(vec: Vector): Vector {
        this.x = vec.x;
        this.y = vec.y;
        return this;
    }

    setXY(x: number, y: number): Vector {
        this.x = x;
        this.y = y;
        return this;
    }

    distance(other: Vector): number {
        const dx = this.x - other.x;
        const dy = this.y - other.y;
        return Math.sqrt(dx * dx + dy * dy);
    }

    distanceXY(x: number, y: number) {
        const dx = this.x - x;
        const dy = this.y - y;
        return Math.sqrt(dx * dx + dy * dy);
    }

    clear(): Vector {
        this.x = 0;
        this.y = 0;
        return this;
    }

    static zero(): Vector {
        return new Vector(0, 0);
    }

    private static clippedRandom(scale: number = 1, minValue: number = 0, maxValue = 1): number {
        const num = (Math.random() - 0.5) * scale;
        if (minValue == 0)
            return Math.min(num, maxValue);

        if (num < 0)
            return Math.max(Math.min(-minValue, num), -maxValue);
        else
            return Math.min(maxValue, Math.max(minValue, num));
    }

    static random(scale: number = 1, minValue: number = 0, maxValue = 1): Vector {
        return new Vector(Vector.clippedRandom(scale, minValue, maxValue), Vector.clippedRandom(scale, minValue, maxValue));
    }
}