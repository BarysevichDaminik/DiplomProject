export interface CodeEntity {
    fullName: string;
    path: string;
    type: number;
    impactReason: string;
    propagationLevel: number;
}

export interface EfficiencyReport {
    runDate: string;
    totalTestsTime: number;
    testsRunCount: number;
    totalTestsCount: number;
    impactedMethods: CodeEntity[];
    timeSaved: number;
}