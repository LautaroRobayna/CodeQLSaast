import { Given, When, Then } from 'cypress-cucumber-preprocessor/steps';

Given('que el sistema tiene cargada la farmacia {string}', (pharmacyName: string) => {
  cy.intercept('GET', '**/api/pharmacy*', {
    statusCode: 200,
    body: [{ id: 1, name: pharmacyName, address: 'Av. Principal 123' }]
  }).as('getPharmacies');
});

Given('la {string} tiene el medicamento {string} que requiere receta con stock de {int} unidades',
  (_pharmacyName: string, _drugName: string, _stock: number) => {
    cy.log('Drug setup handled in visit step');
  }
);

Given('un usuario no autenticado visita la página de reservas {string}', (url: string) => {
  cy.intercept('GET', '**/api/drug?PharmacyId=*', {
    statusCode: 200,
    body: [
      { id: 1, code: 'P-500', name: 'Paracetamol 500mg', symptom: 'Dolor', price: 150, prescription: false },
      { id: 3, code: 'A-500', name: 'Amoxicilina 500mg', symptom: 'Infeccion', price: 300, prescription: true }
    ]
  }).as('getDrugs');

  cy.visit(`http://localhost:4200${url}`);
});

Given('selecciona la farmacia {string} de la lista desplegable {string}', (pharmacyName: string, selector: string) => {
  cy.wait('@getPharmacies');
  cy.get(selector).select(pharmacyName);
  cy.wait('@getDrugs');
});

When('agrega {int} unidad del medicamento {string}', (quantity: number, drugName: string) => {
  cy.contains('td', drugName)
    .parents('tr')
    .within(() => {
      cy.get('input[type="number"]').clear().type(quantity.toString());
      cy.contains('button', 'Agregar').click();
    });
});

Then('la fila del medicamento debe mostrar la etiqueta {string} con el texto {string}',
  (cssClass: string, text: string) => {
    cy.get(cssClass).should('contain', text);
  }
);

Then('debe mostrarse el contenedor de carga de archivos {string}', (selector: string) => {
  cy.get(selector).should('be.visible');
});

Given('completa el formulario con nombre {string} y email {string}', (nombre: string, email: string) => {
  cy.get('#nombre-completo').clear().type(nombre);
  cy.get('#email').clear().type(email);
});

When('arrastra el archivo {string} al elemento input {string}', (fileName: string, selector: string) => {
  const mimeType = fileName.endsWith('.pdf') ? 'application/pdf' : 'text/plain';
  cy.get(selector).selectFile({
    contents: Cypress.Buffer.from('fake content'),
    fileName: fileName,
    mimeType: mimeType
  }, { action: 'drag-drop' });
});

When('hace clic en el botón {string}', (selector: string) => {
  cy.intercept('POST', '**/api/reservation', {
    statusCode: 201,
    body: {
      id: 1,
      code: 'RES-001',
      publicKey: 'PUB-12345',
      status: 'Pendiente',
      prescriptionUploaded: true
    }
  }).as('createReservation');

  cy.get(selector).click();
  cy.wait('@createReservation');
});

Then('la reserva debe crearse con la etiqueta de estado {string} conteniendo el texto {string}',
  (cssClass: string, text: string) => {
    cy.get(cssClass).should('contain', text);
  }
);

Then('el indicador de receta debe decir {string}', (text: string) => {
  cy.get('[data-cy=prescription-status]').should('contain', text);
});

When('intenta confirmar la reserva haciendo clic en {string}', (selector: string) => {
  cy.get(selector).click();
});

Then('el sistema debe mostrar un mensaje de error flotante con el texto {string}', (text: string) => {
  cy.contains('.customToastBody', text).should('be.visible');
});

Then('no debe enviarse la receta al servidor', () => {
  cy.get('.modal.show').should('not.exist');
});
