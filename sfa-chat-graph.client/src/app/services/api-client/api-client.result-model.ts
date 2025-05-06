import { HttpErrorResponse } from "@angular/common/http";

export class BackendError {
  public message!: string;
  public status!: number;
  public title!: string;
  public detail!: string;
}

export class ApiResult<T> {

  public success!: boolean;
  public error?: BackendError;
  public result?: T;

  constructor(error?: BackendError, result?: T) {
    this.success = !error;
    this.error = error;
    this.result = result;
  }

  public static FromError<T>(error: any): ApiResult<T> {
    if (error instanceof HttpErrorResponse) {
      const backendError = new BackendError();
      backendError.message = error.status == 500 ? "An error occurred on the server." : error.error.message;
      backendError.detail = error.status == 500 ? error.error : undefined;
      backendError.status = error.status;
      backendError.title = "Server Side Error";
      return new ApiResult<T>(backendError);
    } else if (error instanceof ErrorEvent) {
      const backendError = new BackendError();
      backendError.message = error.message;
      backendError.status = 0;
      backendError.title = "Client Side Network Error";
      return new ApiResult<T>(backendError);
    } else {
      const backendError = new BackendError();
      backendError.title = "Unknown Error";
      return new ApiResult<T>(backendError, undefined);
    }
  }

  public static FromResult<T>(result: T): ApiResult<T> {
    return new ApiResult<T>(undefined, result);
  }
}
