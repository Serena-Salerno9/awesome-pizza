import { render } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';

import App from './app';

describe('App', () => {
  it('should render successfully', () => {
    const { baseElement } = render(<BrowserRouter><App /></BrowserRouter>);
    expect(baseElement).toBeTruthy();
  });

  it('should render the home title', () => {
    const { getByRole } = render(<BrowserRouter><App /></BrowserRouter>);
    expect(getByRole('heading', { name: 'Awesome Pizza' })).toBeTruthy();
  });
});
