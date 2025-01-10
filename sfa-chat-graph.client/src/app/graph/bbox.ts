import { Vector } from "./vector";

export class BBox {
    public readonly x: number;
    public readonly y: number;
    public readonly width: number;
    public readonly height: number;
  
    constructor(x: number, y: number, width: number, height: number) {
      this.x = x;
      this.y = y;
      this.width = width;
      this.height = height;
    }
  
    center(): Vector {
        return new Vector(this.x + this.width/2, this.y + this.height/2);
    }

    static zero(): BBox{
      return new BBox(0,0,0,0);
    }
  }
  