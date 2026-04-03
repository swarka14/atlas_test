type CompanyFilterProps = {
  companies: string[];
  selectedCompanyName: string | null;
  onChange: (companyName: string | null) => void;
  disabled?: boolean;
};

const ALL_COMPANIES_VALUE = "__all__";

export function CompanyFilter({
  companies,
  selectedCompanyName,
  onChange,
  disabled
}: CompanyFilterProps) {
  const handleCompanyChange = (value: string) => {
    onChange(value === ALL_COMPANIES_VALUE ? null : value);
  };

  return (
    <label className="company-filter">
      <span>Company</span>
      <select
        value={selectedCompanyName ?? ALL_COMPANIES_VALUE}
        onChange={(event) => handleCompanyChange(event.target.value)}
        disabled={disabled}
      >
        <option value={ALL_COMPANIES_VALUE}>All Companies</option>
        {companies.map((company) => (
          <option key={company} value={company}>
            {company}
          </option>
        ))}
      </select>
    </label>
  );
}


