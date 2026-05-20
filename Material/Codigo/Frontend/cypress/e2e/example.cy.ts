describe('PharmaGo - Test de Ejemplo', () => {
  beforeEach(() => {
    // Visitar la página principal antes de cada test
    cy.visit('http://localhost:4200/');
  });

  it('debe cargar la página principal correctamente', () => {
    cy.url().should('include', 'localhost:4200');
  });

  it('debe mostrar el título de la aplicación', () => {
    // Ajusta el selector según tu aplicación
    cy.get('h1, .app-title, [data-cy="app-title"]').should('exist');
  });

  it('debe navegar a diferentes rutas', () => {
    // Ejemplo de navegación - ajusta según tus rutas
    cy.get('a[href="/login"]').first().click();
    cy.url().should('include', '/login');
  });
});

describe('PharmaGo - Test de Login', () => {
  beforeEach(() => {
    cy.visit('http://localhost:4200/login');
  });

  it('debe mostrar el formulario de login', () => {
    cy.get('input[name="username"], input[type="email"], input[formControlName="username"]').should('exist');
    cy.get('input[name="password"], input[type="password"], input[formControlName="password"]').should('exist');
    cy.get('button[type="submit"]').should('exist');
  });

  it('debe validar campos vacíos', () => {
    cy.get('button[type="submit"]').click();
    // Verifica que se muestren mensajes de error o que no se permita el envío
  });

  // Ejemplo con fixture
  it('debe permitir login con credenciales válidas', () => {
    cy.fixture('example').then((user) => {
      cy.get('input[name="username"], input[type="email"], input[formControlName="username"]')
        .type(user.username);
      cy.get('input[name="password"], input[type="password"], input[formControlName="password"]')
        .type(user.password);
      cy.get('button[type="submit"]').click();

      // Verifica redirección o mensaje de éxito
      // cy.url().should('include', '/dashboard');
    });
  });
});
