import { Given, When, Then } from 'cypress-cucumber-preprocessor/steps';

Given('existe una reserva en estado {string} con clave pública {string} y fecha de expiración {string}',
  (status: string, publicKey: string, expirationDate: string) => {
    cy.intercept('GET', '**/api/reservation*', {
      statusCode: 200,
      body: {
        id: 1,
        code: 'RES-001',
        publicKey: publicKey,
        userEmail: 'cliente@example.com',
        reservationDate: '2026-05-23T10:00:00',
        expirationDate: expirationDate,
        status: status,
        prescriptionUploaded: false,
        details: [
          { id: 1, drugCode: 'PAR-500', drugName: 'Paracetamol 500mg', quantity: 3, requiresPrescription: false }
        ]
      }
    }).as('getReservation');
  }
);

Given('el cliente visita la página {string}', (url: string) => {
  cy.intercept('PUT', '**/api/reservation/cancel*', {
    statusCode: 200,
    fixture: 'cancel-response.json'
  }).as('cancelReservation');

  cy.visit(`http://localhost:4200${url}`);
});

Given('ingresa la clave pública {string} en el campo {string}', (publicKey: string, selector: string) => {
  cy.get(selector).clear().type(publicKey);
});

Given('hace clic en {string}', (selector: string) => {
  cy.get(selector).click();
  cy.wait('@getReservation');
});

Given('el sistema muestra el botón {string}', (selector: string) => {
  cy.get(selector).should('be.visible');
});

When('hace clic en {string}', (selector: string) => {
  cy.get(selector).click();
});

Then('el sistema debe cambiar el estado de la reserva a {string}', (estadoEsperado: string) => {
  cy.wait('@cancelReservation').then((interception: any) => {
    expect(interception.response).to.exist;
    expect(interception.response.body.status).to.equal(estadoEsperado);
  });
  cy.get('[data-cy=reservation-status]').should('contain', estadoEsperado);
});

Then('el botón {string} no debe estar visible', (selector: string) => {
  cy.get(selector).should('not.exist');
});
