import { Given, When, Then } from 'cypress-cucumber-preprocessor/steps';

Given('que el sistema tiene cargada la farmacia {string}', (pharmacyName: string) => {
  cy.log(`Farmacia ${pharmacyName} registrada en sistema`);
});

Given('la {string} tiene el medicamento {string} que requiere receta con stock de {int} unidades',
  (pharmacyName: string, drugName: string, stock: number) => {
    cy.log(`${pharmacyName} tiene ${drugName} con stock ${stock} (requiere receta)`);
  }
);

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

Given('existe una reserva en estado {string} con clave pública {string} y fecha de expiración {string}',
  (status: string, publicKey: string, expirationDate: string) => {
    cy.intercept('GET', '**/api/reservation*', {
      statusCode: 200,
      body: {
        id: 2,
        code: 'RES-TEST-002',
        publicKey: publicKey,
        status: status,
        userEmail: 'carlos@example.com',
        pharmacyId: 1,
        pharmacyName: 'Farmacia Central',
        reservationDate: '2026-05-15T10:00:00',
        expirationDate: expirationDate,
        details: [
          { id: 1, drugCode: 'P-500', drugName: 'Paracetamol 500mg', quantity: 2, requiresPrescription: false }
        ]
      }
    }).as('getReservation');
  }
);

Given('no existe ninguna reserva con la clave pública {string}', (_publicKey: string) => {
  cy.intercept('GET', '**/api/reservation*', {
    statusCode: 404,
    body: { message: 'Reserva no encontrada.' }
  }).as('getReservation');
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

Then('debe mostrar el mensaje {string}', (message: string) => {
  cy.get('[data-cy=reservation-message]').should('contain', message);
});

Then('debe mostrar la fecha de expiración {string}', (date: string) => {
  cy.get('[data-cy=expiration-date]').should('contain', date);
});

Then('debe mostrar el mensaje de error {string}', (message: string) => {
  cy.get('[data-cy=error-message]').should('contain', message);
});

Then('no debe mostrar ningún aviso de receta faltante', () => {
  cy.get('[data-cy=prescription-warning]').should('not.exist');
});
