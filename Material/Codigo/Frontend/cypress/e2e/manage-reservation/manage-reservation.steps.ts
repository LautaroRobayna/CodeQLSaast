import { Given, When, Then } from 'cypress-cucumber-preprocessor/steps';


Given('existe una reserva en estado {string} para el email {string} en {string} con los siguientes medicamentos:',
  (status: string, email: string, pharmacyName: string, dataTable: { rawTable: string[][] }) => {
    const rows = dataTable.rawTable.slice(1);
    const details = rows.map((row: string[], index: number) => ({
      id: index + 1,
      drugCode: 'AMX-500',
      drugName: row[0],
      quantity: parseInt(row[1]),
      requiresPrescription: true
    }));

    cy.intercept('GET', '**/api/reservation*', {
      statusCode: 200,
      body: {
        id: 1,
        code: 'RES-TEST-001',
        publicKey: 'CLAVE-PUBLICA-TEST',
        status: status,
        userEmail: email,
        pharmacyId: 1,
        pharmacyName: pharmacyName,
        reservationDate: '2026-05-23T10:00:00',
        details: details
      }
    }).as('getReservation');
  }
);


Given('el cliente visita la página {string}', (url: string) => {
  cy.visit(`http://localhost:4200${url}`);
});

When('ingresa la clave pública {string} en el campo {string}', (publicKey: string, selector: string) => {
  cy.get(selector).type(publicKey);
});

When('hace clic en {string}', (selector: string) => {
  cy.get(selector).click();
});

Then('el sistema debe mostrar la reserva en estado {string}', (status: string) => {
  cy.wait('@getReservation');
  cy.get('[data-cy=reservation-status]').should('contain', status);
});

Then('debe mostrar el aviso {string} para el medicamento {string}', (warning: string, drugName: string) => {
  cy.get('[data-cy=prescription-warning]').should('contain', warning);
  cy.get('[data-cy=prescription-warning]').should('contain', drugName);
});
