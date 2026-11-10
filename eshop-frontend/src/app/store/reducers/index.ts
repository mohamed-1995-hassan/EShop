import { isDevMode } from '@angular/core';
import { ActionReducerMap, MetaReducer } from '@ngrx/store';

export interface AppState {
  // Define your state interface here
}

export const reducers: ActionReducerMap<AppState> = {
  // Add your feature reducers here
};

export const metaReducers: MetaReducer<AppState>[] = isDevMode() ? [] : [];
