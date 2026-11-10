import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { NotificationService } from '@core/services/notification.service';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const notification = inject(NotificationService);
  
  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      let errorMessage = 'An unexpected error occurred';
      
      if (error.error instanceof ErrorEvent) {
        // Client-side error
        errorMessage = error.error.message;
      } else {
        // Server-side error
        switch (error.status) {
          case 0:
            errorMessage = 'Unable to connect to server. Please check your connection.';
            break;
          case 400:
            errorMessage = error.error?.detail || error.error?.message || 'Bad request';
            break;
          case 401:
            errorMessage = 'Session expired. Please login again.';
            break;
          case 403:
            errorMessage = 'You do not have permission to perform this action.';
            break;
          case 404:
            errorMessage = error.error?.detail || 'Resource not found';
            break;
          case 422:
            errorMessage = error.error?.detail || 'Validation failed';
            break;
          case 429:
            errorMessage = 'Too many requests. Please try again later.';
            break;
          case 500:
            errorMessage = 'Server error. Please try again later.';
            break;
          default:
            errorMessage = error.error?.detail || error.error?.message || `Error: ${error.status}`;
        }
      }
      
      // Show notification (skip for certain errors that are handled elsewhere)
      if (error.status !== 401) {
        notification.error(errorMessage);
      }
      
      return throwError(() => ({ ...error, userMessage: errorMessage }));
    })
  );
};
