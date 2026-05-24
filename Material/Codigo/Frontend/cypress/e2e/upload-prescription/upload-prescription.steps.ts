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
